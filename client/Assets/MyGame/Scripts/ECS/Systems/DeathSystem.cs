using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 播放死亡动画 禁止移动/攻击 延迟销毁实体或标记待移除
public class DeathSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.entities)
        {
            // 不再强制要求 AttackRequestComponent。
            // 非房主怪物不添加 AttackRequestComponent，
            // 如果这里继续要求它，就会导致远端怪物 Health.IsDead 后无法进入死亡销毁流程。
            if (!entity.Has<HealthComponent>() ||
                !entity.Has<AnimationComponent>() ||
                !entity.Has<TransformComponent>() ||
                !entity.Has<DeathStateComponent>() ||
                !entity.Has<AnimationCommandComponent>())
                continue;

            var health = entity.Get<HealthComponent>();
            var anim = entity.Get<AnimationComponent>();
            var trans = entity.Get<TransformComponent>();
            var deathState = entity.Get<DeathStateComponent>();
            var animCmd = entity.Get<AnimationCommandComponent>();

            if (!health.IsDead)
                continue;

            // 第一次进入死亡
            if (!deathState.HasProcessedDeath)
            {
                deathState.HasProcessedDeath = true;
                deathState.DestroyTimer = deathState.DestroyDelay;

                anim.IsDead = true;
                trans.CanMove = false;

                // AttackRequestComponent 变成可选组件
                if (entity.Has<AttackRequestComponent>())
                {
                    var attackReq = entity.Get<AttackRequestComponent>();
                    attackReq.IsRequesting = false;
                    attackReq.BufferTimer = 0f;
                    entity.Add(attackReq);
                }

                // 只触发一次死亡动画
                animCmd.TriggerDeath = true;

                // 如果死亡对象处于攻击状态
                if (entity.Has<AttackRuntimeComponent>())
                {
                    var runtime = entity.Get<AttackRuntimeComponent>();
                    runtime.IsAttacking = false;
                    runtime.ComboQueued = false;
                    runtime.CanQueueNext = false;
                    runtime.CurrentComboIndex = 0;
                    runtime.DamageAppliedThisAction = false;
                    entity.Add(runtime);
                }

                // 停掉攻击位移
                if (entity.Has<AttackMotionComponent>())
                {
                    var motion = entity.Get<AttackMotionComponent>();
                    AttackMotionUtility.StopMotion(ref motion);
                    entity.Add(motion);
                }

                // 处于受击状态
                if (entity.Has<HitReactionStateComponent>())
                {
                    var hitState = entity.Get<HitReactionStateComponent>();
                    hitState.IsInHitStun = false;
                    hitState.HitStunTimer = 0f;
                    hitState.KnockbackVelocity = Vector3.zero;
                    hitState.KnockbackDamping = 0f;
                    entity.Add(hitState);
                }

                entity.Add(anim);
                entity.Add(trans);
                entity.Add(deathState);
                entity.Add(animCmd);
                continue;
            }

            // 已死亡，开始倒计时
            deathState.DestroyTimer -= deltaTime;

            // 标记需要删除
            if (deathState.DestroyTimer <= 0)
            {
                entity.Add(new PendingDestroyComponent
                {
                    NeedDestroy = true
                });
            }

            entity.Add(deathState);
        }
    }
}
