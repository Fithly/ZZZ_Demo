using UnityEngine;

/// <summary>
/// 负责：
/// 1. 检测急刹车 / 反向输入 -> TriggerRunTurn
/// 2. 检测正常停步 -> TriggerRunEnd
/// 3. Run_End 期间锁定 CanMove / CanRotate
/// 4. 收到动画事件后解锁
/// 
/// 只处理玩家输入角色（有 InputComponent 的实体）
/// </summary>
public class LocomotionAnimationSystem : ISystem
{
    // 输入死区
    private const float MoveThreshold = 0.15f;

    // 方向点乘阈值：
    // dot = -1 表示完全反向，0 表示垂直，1 表示同向
    // 这里 <= -0.6f 视为明显反向
    private const float ReverseDotThreshold = -0.6f;

    // 从“松开”到真正触发 Run_End 之前，给一个很短观察窗口
    // 用来接住 W -> S / D -> A 这种切向输入，中间可能出现的一帧 0
    private const float ReverseInputGraceTime = 0.2f;

    // 只有速度达到最大移动速度的一定比例，才允许触发 Run_End
    // 防止轻点移动键也播放急停动画
    private const float RunEndMinSpeedRatio = 0.9f;

    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.entities)
        {
            if (!entity.Has<InputComponent>() ||
                !entity.Has<MoveIntentComponent>() ||
                !entity.Has<TransformComponent>() ||
                !entity.Has<VelocityComponent>() ||
                !entity.Has<AnimationCommandComponent>() ||
                !entity.Has<AnimationEventComponent>() ||
                !entity.Has<LocomotionStateComponent>() ||
                !entity.Has<HealthComponent>())
                continue;

            var moveIntent = entity.Get<MoveIntentComponent>();
            var trans = entity.Get<TransformComponent>();
            var velocity = entity.Get<VelocityComponent>();
            var cmd = entity.Get<AnimationCommandComponent>();
            var evt = entity.Get<AnimationEventComponent>();
            var locomotion = entity.Get<LocomotionStateComponent>();
            var health = entity.Get<HealthComponent>();

            bool isAttacking = false;
            if (entity.Has<AttackRuntimeComponent>())
            {
                var runtime = entity.Get<AttackRuntimeComponent>();
                isAttacking = runtime.IsAttacking;
            }

            bool isInHitStun = false;
            if (entity.Has<HitReactionStateComponent>())
            {
                var hitState = entity.Get<HitReactionStateComponent>();
                isInHitStun = hitState.IsInHitStun;
            }

            // 死亡 / 攻击 / 受击 时，不处理移动收招逻辑
            if (health.IsDead || isAttacking || isInHitStun)
            {
                locomotion.PendingRunEnd = false;
                locomotion.PendingRunEndTimer = 0f;
                locomotion.IsRunEndPlaying = false;

                // 状态被打断时，重新累计奔跑速度条件
                locomotion.HasReachedRunSpeed = false;

                locomotion.WasMovingLastFrame = moveIntent.MoveInput.sqrMagnitude > MoveThreshold * MoveThreshold;

                if (moveIntent.MoveInput.sqrMagnitude > MoveThreshold * MoveThreshold)
                    locomotion.LastNonZeroMoveInput = moveIntent.MoveInput.normalized;

                trans.CanRotate = true;

                entity.Add(locomotion);
                entity.Add(trans);
                continue;
            }

            // Run_End 播放结束 -> 解锁
            if (evt.RunEndFinished)
            {
                evt.RunEndFinished = false;

                locomotion.IsRunEndPlaying = false;
                locomotion.PendingRunEnd = false;
                locomotion.PendingRunEndTimer = 0f;

                // Run_End 播完后，重新累计奔跑速度条件
                locomotion.HasReachedRunSpeed = false;

                trans.CanMove = true;
                trans.CanRotate = true;
            }

            // Run_End 播放期间持续锁定
            if (locomotion.IsRunEndPlaying)
            {
                trans.CanMove = false;
                trans.CanRotate = false;

                entity.Add(evt);
                entity.Add(trans);
                entity.Add(locomotion);
                continue;
            }

            Vector2 currentInput = moveIntent.MoveInput;
            bool hasCurrentInput = currentInput.sqrMagnitude > MoveThreshold * MoveThreshold;
            Vector2 currentDir = hasCurrentInput ? currentInput.normalized : Vector2.zero;

            // 判断当前水平速度是否已经达到奔跑速度
            float planarSpeed = velocity.PlanarVelocity.magnitude;
            float requiredRunEndSpeed = Mathf.Max(0.01f, velocity.MoveSpeed * RunEndMinSpeedRatio);
            bool hasReachedRunSpeedNow = planarSpeed >= requiredRunEndSpeed;

            // 只有有输入且速度真正达到奔跑速度，才记录可以播放 Run_End
            if (hasCurrentInput && hasReachedRunSpeedNow)
            {
                locomotion.HasReachedRunSpeed = true;
            }

            Vector2 lastDir = locomotion.LastNonZeroMoveInput;
            bool hasLastDir = lastDir.sqrMagnitude > 0.0001f;

            bool reverseNow = false;
            if (hasCurrentInput && hasLastDir)
            {
                float dot = Vector2.Dot(lastDir, currentDir);
                reverseNow = dot <= ReverseDotThreshold;
            }

            // 1. 直接反向输入：立即 TriggerRunTurn
            if (locomotion.WasMovingLastFrame && reverseNow)
            {
                locomotion.PendingRunEnd = false;
                locomotion.PendingRunEndTimer = 0f;
                locomotion.WasMovingLastFrame = true;
                locomotion.LastNonZeroMoveInput = currentDir;

                entity.Add(evt);
                entity.Add(trans);
                entity.Add(locomotion);
                continue;
            }

            // 2. 上一帧在移动，这一帧没输入
            //    不立刻 Run_End，先开一个短窗口等反向输入
            // 只有真正达到过奔跑速度，才进入 Run_End 观察窗口
            if (locomotion.WasMovingLastFrame && !hasCurrentInput && !locomotion.PendingRunEnd)
            {
                if (locomotion.HasReachedRunSpeed)
                {
                    locomotion.PendingRunEnd = true;
                    locomotion.PendingRunEndTimer = ReverseInputGraceTime;
                }
                else
                {
                    // 轻点移动键，速度没到奔跑速度，不触发 Run_End
                    locomotion.PendingRunEnd = false;
                    locomotion.PendingRunEndTimer = 0f;
                    locomotion.WasMovingLastFrame = false;
                }
            }

            // 3. Pending 窗口里，如果来了反向输入 -> Run_Turn
            if (locomotion.PendingRunEnd)
            {
                locomotion.PendingRunEndTimer -= deltaTime;

                if (hasCurrentInput && reverseNow)
                {
                    locomotion.PendingRunEnd = false;
                    locomotion.PendingRunEndTimer = 0f;
                    locomotion.WasMovingLastFrame = true;
                    locomotion.LastNonZeroMoveInput = currentDir;

                    entity.Add(evt);
                    entity.Add(trans);
                    entity.Add(locomotion);
                    continue;
                }

                // 4. 窗口结束仍没等到反向输入 -> 真正触发 Run_End
                if (locomotion.PendingRunEndTimer <= 0f)
                {
                    cmd.TriggerRunEnd = true;

                    locomotion.PendingRunEnd = false;
                    locomotion.PendingRunEndTimer = 0f;
                    locomotion.IsRunEndPlaying = true;
                    locomotion.WasMovingLastFrame = false;

                    // 本次急停已经消费，下一次必须重新达到奔跑速度
                    locomotion.HasReachedRunSpeed = false;

                    trans.CanMove = false;
                    trans.CanRotate = false;

                    entity.Add(cmd);
                    entity.Add(evt);
                    entity.Add(trans);
                    entity.Add(locomotion);
                    continue;
                }
            }

            // 更新最近一次非零方向
            if (hasCurrentInput)
            {
                locomotion.LastNonZeroMoveInput = currentDir;
            }

            locomotion.WasMovingLastFrame = hasCurrentInput;

            entity.Add(cmd);
            entity.Add(evt);
            entity.Add(trans);
            entity.Add(locomotion);
        }
    }
}