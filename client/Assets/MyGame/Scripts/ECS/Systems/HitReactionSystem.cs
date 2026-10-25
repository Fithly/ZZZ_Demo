using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 处理受击硬直 / 击退 / 受击动画
/// 消费 HitReactionBufferComponent
/// 如果实体已死，忽略受击
/// 打断攻击
/// 禁止移动
/// 触发受击动画
/// 刷新硬直时间
/// 写入击退速度
/// 硬直结束后恢复
/// </summary>
public class HitReactionSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.entities)
        {
            if (!entity.Has<HitReactionStateComponent>() ||
                !entity.Has<TransformComponent>() ||
                !entity.Has<HealthComponent>())
                continue;

            var state = entity.Get<HitReactionStateComponent>();
            var trans = entity.Get<TransformComponent>();
            var health = entity.Get<HealthComponent>();

            if (health.IsDead)
            {
                state.IsInHitStun = false;
                state.HitStunTimer = 0;
                state.KnockbackVelocity = Vector3.zero;
                state.KnockbackDamping = 0;

                entity.Add(state);
                continue;
            }

            bool hasNewRequests = entity.Has<HitReactionBufferComponent>();
            if (hasNewRequests)
            {
                var requestBuffer = entity.Get<HitReactionBufferComponent>();

                if (requestBuffer.Requests != null && requestBuffer.Requests.Count > 0)
                {
                    //Debug.Log("攻击请求数量“" + requestBuffer.Requests.Count);

                    // 获得最后一段受击的 硬直时间 / 击退速度
                    HitReactionRequestInfo chosen = requestBuffer.Requests[requestBuffer.Requests.Count - 1];
                    float maxStun = chosen.StunDuration;
                    float maxKnockback = chosen.KnockbackSpeed;

                    for (int i = 0; i < requestBuffer.Requests.Count; i++)
                    {
                        var req = requestBuffer.Requests[i];

                        // 在遍历过程中获得最大 硬直时间 / 击退速度
                        if (req.StunDuration > maxStun)
                            maxStun = req.StunDuration;

                        if (req.KnockbackSpeed > maxKnockback)
                        {
                            maxKnockback = req.KnockbackSpeed;
                            chosen = req;
                        }
                    }

                    // 进入受击硬直状态
                    EnterHitReaction(entity, ref state, ref trans, maxStun, chosen);
                }

                // 清除受击请求（已消费）
                requestBuffer.Requests.Clear();
                entity.Add(requestBuffer);
                entity.Remove<HitReactionBufferComponent>();
            }

            // 处于受击硬直状态
            if (state.IsInHitStun)
            {
                state.HitStunTimer -= deltaTime;
                // 硬直时间结束
                if (state.HitStunTimer <= 0f)
                {
                    state.IsInHitStun = false;
                    state.HitStunTimer = 0f;
                    state.KnockbackVelocity = Vector3.zero;
                    state.KnockbackDamping = 0f;

                    bool shouldRestoreMove = true;
                    // 处于攻击状态则不需要恢复移动
                    if (entity.Has<AttackRuntimeComponent>())
                    {
                        var runtime = entity.Get<AttackRuntimeComponent>();
                        if(runtime.IsAttacking)
                            shouldRestoreMove = false;
                    }

                    // 非攻击状态可以恢复移动
                    if (shouldRestoreMove)
                        trans.CanMove = true;
                }
                // 硬直尚未结束
                else
                {
                    // MoveTowards计算从 current 指向 target 的向量，maxDistanceDelta：本次移动允许的最大距离（通常等于 速度 * 时间）
                    // 如果两点之间的距离 小于等于 maxDistanceDelta，则直接返回 target、
                    // 如果两点之间的距离 大于 maxDistanceDelta，则沿着从 current 到 target 的方向，移动 maxDistanceDelta 的距离，并返回新的位置
                    // 初速度 0 每秒衰减速度
                    state.KnockbackVelocity = Vector3.MoveTowards(
                        state.KnockbackVelocity,
                        Vector3.zero,
                        state.KnockbackDamping * deltaTime);
                }
            }

            entity.Add(state);
            entity.Add(trans);
        }
    }

    private static void EnterHitReaction(Entity entity, ref HitReactionStateComponent state, 
                                        ref TransformComponent trans, float stunDuration, HitReactionRequestInfo chosen)
    {
        trans.CanMove = false;


        // 执行动画命令 （不在受击状态下再去执行）
        if (entity.Has<AnimationCommandComponent>() && !state.IsInHitStun)
        {
            state.IsInHitStun = true;

            var cmd = entity.Get<AnimationCommandComponent>();
            cmd.TriggerAttack = false;
            cmd.AttackEnd = true;
            cmd.TriggerHit = true;

            entity.Add(cmd);
        }
        state.HitStunTimer = Mathf.Max(state.HitStunTimer, stunDuration);
        state.KnockbackVelocity = chosen.HitDirection * chosen.KnockbackSpeed;
        state.KnockbackDamping = chosen.KnockbackDamping;

        // 将角色面朝被攻击的方向
        FaceOppositeOfHit(ref trans, chosen.HitDirection);

        // 取消攻击请求
        if (entity.Has<AttackRequestComponent>())
        {
            var attackReq = entity.Get<AttackRequestComponent>();
            attackReq.IsRequesting = false;
            attackReq.BufferTimer = 0f;
            entity.Add(attackReq);
        }

        // 取消攻击状态
        if (entity.Has<AttackRuntimeComponent>())
        {
            var runtime = entity.Get<AttackRuntimeComponent>();
            runtime.IsAttacking = false;
            runtime.ComboQueued = false;
            runtime.CanQueueNext = false;
            runtime.CurrentComboIndex = 0;
            runtime.ComboResetTimer = 0f;
            runtime.DamageAppliedThisAction = false;
            entity.Add(runtime);
        }

        // 停止攻击位移
        if (entity.Has<AttackMotionComponent>())
        {
            var motion = entity.Get<AttackMotionComponent>();
            AttackMotionUtility.StopMotion(ref motion);
            entity.Add(motion);
        }
    }

    private static void FaceOppositeOfHit(ref TransformComponent trans, Vector3 hitDir)
    {
        hitDir.y = 0f;
        if (hitDir.sqrMagnitude <= 0.0001f)
            return;

        Vector3 faceDir = -hitDir.normalized;
        trans.Rotation = Quaternion.LookRotation(faceDir);
    }
}
