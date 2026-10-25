using UnityEngine;

public class AnimationSystem : ISystem
{
    private const float MoveInputThreshold = 0.15f;
    private const float MoveSpeedThreshold = 0.05f;

    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.entities)
        {
            if (!entity.Has<AnimationComponent>() ||
                !entity.Has<TransformComponent>())
                continue;

            var anim = entity.Get<AnimationComponent>();

            // 将攻击和死亡的优先级放在最前 以防攻击和死亡时动画依旧处于移动
            bool isAttacking = false;
            if (entity.Has<AttackRuntimeComponent>())
            {
                var runtime = entity.Get<AttackRuntimeComponent>();
                isAttacking = runtime.IsAttacking;
            }

            bool isDead = false;
            if (entity.Has<HealthComponent>())
            {
                var healthComp = entity.Get<HealthComponent>();
                isDead = healthComp.IsDead;
            }

            if (isDead)
            {
                anim.MoveSpeed = 0f;
                anim.HasMoveInput = false;
                anim.CanEnterIdle = false;
                anim.IsDead = false;

                entity.Add(anim);
                continue;
            }

            if (isAttacking)
            {
                anim.MoveSpeed = 0f;
                anim.HasMoveInput = false;
                anim.CanEnterIdle = false;
                anim.IsDead = false;

                entity.Add(anim);
                continue;
            }

            float planarSpeed = 0f;
            if (entity.Has<VelocityComponent>())
            {
                var velocity = entity.Get<VelocityComponent>();
                Vector3 planar = velocity.PlanarVelocity;
                planar.y = 0f;
                planarSpeed = planar.magnitude;
            }

            if (planarSpeed < MoveSpeedThreshold)
            {
                planarSpeed = 0f;
            }

            // 是否存在移动输入
            bool hasMoveInput = false;
            if (entity.Has<MoveIntentComponent>())
            {
                var moveIntent = entity.Get<MoveIntentComponent>();
                hasMoveInput = moveIntent.MoveInput.sqrMagnitude > MoveInputThreshold * MoveInputThreshold;
            }

            // 有显著的水平速度
            bool hasPlanarSpeed = planarSpeed > MoveSpeedThreshold;

            bool isPendingRunEnd = false;           // 是否打开判定窗口
            bool isRunEndPlaying = false;           // 是否执行RunEnd

            if (entity.Has<LocomotionStateComponent>())
            {
                var locomotion = entity.Get<LocomotionStateComponent>();
                isPendingRunEnd = locomotion.PendingRunEnd;
                isRunEndPlaying = locomotion.IsRunEndPlaying;
            }

            bool isTriggeringRunEnd = false;
            if (entity.Has<AnimationCommandComponent>())
            {
                var cmd = entity.Get<AnimationCommandComponent>();
                isTriggeringRunEnd = cmd.TriggerRunEnd;
            }

            // 只有在没有输入、没有速度、没有等待 Run_End、没有播放 Run_End、
            // 并且本帧没有触发 RunEndTrigger 时，才允许直接回 Idle。
            bool canEnterIdle =
                !hasMoveInput &&
                !hasPlanarSpeed &&
                !isPendingRunEnd &&
                !isRunEndPlaying &&
                !isTriggeringRunEnd;

            anim.MoveSpeed = planarSpeed;

            // 本地：InputSystem 写 MoveIntent。
            // 远端：BattleInput 写 MoveIntent。
            // 因此这里本地和远端可以统一判断。
            // HasMoveInput 用于 Idle -> Walk_Start / Walk_Start -> Walk_Loop。
            // 它不再单纯依赖速度，避免 W -> S 时速度归零导致动画误回 Idle。
            anim.HasMoveInput = hasMoveInput || hasPlanarSpeed;

            anim.CanEnterIdle = canEnterIdle;
            anim.IsDead = false;

            entity.Add(anim);
        }
    }
}