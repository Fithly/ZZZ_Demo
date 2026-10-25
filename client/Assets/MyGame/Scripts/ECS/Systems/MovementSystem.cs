using Unity.VisualScripting;
using UnityEngine;

public class MovementSystem : ISystem
{
    public Transform cameraTransform;

    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.entities)
        {
            if (!entity.Has<MoveIntentComponent>() ||
                !entity.Has<VelocityComponent>() ||
                !entity.Has<TransformComponent>())
                continue;

            // 远端网络玩家的位置和速度由 NetworkBattleEntry 同步，不再走本地移动模拟
            // 否则远端玩家的 MoveIntent 默认为 0，会把网络同步来的 PlanarVelocity 覆盖成 0，
            // 进而导致 AnimationSystem 判断不到移动速度，走路/奔跑动画不播放。
            if (entity.Has<NetworkIdentityComponent>())
            {
                var net = entity.Get<NetworkIdentityComponent>();
                if (!net.IsLocalPlayer)
                    continue;
            }

            var moveIntent = entity.Get<MoveIntentComponent>();
            var velocity = entity.Get<VelocityComponent>();
            var trans = entity.Get<TransformComponent>();

            Vector3 targetInputPlanarVelocity = Vector3.zero;     // 目标输入速度
            Vector3 moveDir = Vector3.zero;                       // 移动方向

            // 先算普通输入移动（受 CanMove 控制）
            if (trans.CanMove)
            {
                // 玩家
                if (entity.Has<InputComponent>())
                {
                    Vector3 camForward = cameraTransform.forward;
                    Vector3 camRight = cameraTransform.right;
                    camForward.y = 0f;
                    camRight.y = 0f;
                    camForward.Normalize();
                    camRight.Normalize();

                    targetInputPlanarVelocity =
                        camForward * moveIntent.MoveInput.y +
                        camRight * moveIntent.MoveInput.x;

                    moveDir = targetInputPlanarVelocity;
                }
                // 怪物AI
                else
                {
                    moveDir = trans.Rotation * new Vector3(moveIntent.MoveInput.x, 0f, moveIntent.MoveInput.y);
                    targetInputPlanarVelocity = moveDir;
                }

                if (targetInputPlanarVelocity.sqrMagnitude > 0.01f)
                {
                    targetInputPlanarVelocity = targetInputPlanarVelocity.normalized * velocity.MoveSpeed;
                }
                else
                {
                    targetInputPlanarVelocity = Vector3.zero;
                }
            }

            // 旋转单独受 CanRotate 控制
            if (trans.CanRotate && moveDir.sqrMagnitude > 0.01f)
            {
                trans.Rotation = Quaternion.Slerp(
                    trans.Rotation,
                    Quaternion.LookRotation(moveDir),
                    15f * deltaTime);
            }

            // 渐进加速 / 减速
            float acceleration = velocity.Acceleration > 0f ? velocity.Acceleration : 25f;
            float deceleration = velocity.Deceleration > 0f ? velocity.Deceleration : 35f;
            float stopSpeedEpsilon = velocity.StopSpeedEpsilon > 0f ? velocity.StopSpeedEpsilon : 0.05f;

            // 是否有输入速度值
            bool hasTargetInputVelocity = targetInputPlanarVelocity.sqrMagnitude > stopSpeedEpsilon * stopSpeedEpsilon;
            // 获得加速 / 减速度
            float speedChangeRate = hasTargetInputVelocity ? acceleration : deceleration;

            // 不再直接 velocity.PlanarVelocity = MoveSpeed
            // 而是逐渐靠近目标速度
            Vector3 smoothInputPlanarVelocity = Vector3.MoveTowards(
                velocity.PlanarVelocity,
                targetInputPlanarVelocity,
                speedChangeRate * deltaTime);

            // 极小速度直接归零，避免 Animator MoveSpeed 出现 3.993e-26 这种残留
            if (smoothInputPlanarVelocity.sqrMagnitude < stopSpeedEpsilon * stopSpeedEpsilon)
            {
                smoothInputPlanarVelocity = Vector3.zero;
            }

            // 再叠加/覆盖攻击位移
            Vector3 attackMotionVelocity = Vector3.zero;
            bool lockMoveByAttackMotion = false;

            if (entity.Has<AttackMotionComponent>())
            {
                var motion = entity.Get<AttackMotionComponent>();
                attackMotionVelocity = motion.FrameVelocity;

                // 获得攻击位移配置
                AttackMotionClip clip;
                if (motion.IsActive && AttackMotionUtility.TryGetClip(motion, motion.ActiveComboIndex, out clip))
                {
                    // 锁定Input移动
                    lockMoveByAttackMotion = clip.LockMove;
                }
            }

            // 攻击位移覆盖 / 叠加
            if (lockMoveByAttackMotion)
                // 水平速度由攻击位移速度接管
                velocity.PlanarVelocity = attackMotionVelocity;
            else
                velocity.PlanarVelocity = smoothInputPlanarVelocity + attackMotionVelocity;

            // 受击击退覆盖水平位移
            if (entity.Has<HitReactionStateComponent>())
            {
                var hitState = entity.Get<HitReactionStateComponent>();
                if (hitState.IsInHitStun)
                {
                    velocity.PlanarVelocity = hitState.KnockbackVelocity;
                }
            }

            // 最终速度再次做一次极小值归零
            if (velocity.PlanarVelocity.sqrMagnitude < stopSpeedEpsilon * stopSpeedEpsilon)
            {
                velocity.PlanarVelocity = Vector3.zero;
            }

            // 重力
            if (trans.IsGrounded && velocity.VerticalVelocity < 0f)
            {
                velocity.VerticalVelocity = -2f;
            }
            else
            {
                velocity.VerticalVelocity += velocity.Gravity * deltaTime;
            }

            entity.Add(velocity);
            entity.Add(trans);
        }
    }
}