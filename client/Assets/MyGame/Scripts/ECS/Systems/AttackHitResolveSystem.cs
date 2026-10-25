using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 真正命中结算：
/// 当动画事件发出 HitFrame 时，检测攻击范围内敌人，向目标追加 DamageBuffer
/// 支持一次攻击动画内出现多个命中帧（多段伤害）
/// </summary>
public class AttackHitResolveSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.entities)
        {
            if (!entity.Has<AnimationEventComponent>() ||
                !entity.Has<AttackComponent>() ||
                !entity.Has<AttackRuntimeComponent>() ||
                !entity.Has<TransformComponent>() ||
                !entity.Has<CampComponent>() ||
                !entity.Has<HealthComponent>())
                continue;

            var evt = entity.Get<AnimationEventComponent>();
            var attack = entity.Get<AttackComponent>();
            var runtime = entity.Get<AttackRuntimeComponent>();
            var selfTransform = entity.Get<TransformComponent>();
            var selfCamp = entity.Get<CampComponent>();
            var selfHealth = entity.Get<HealthComponent>();

            if (!evt.HitFrame || selfHealth.IsDead || !runtime.IsAttacking)
                continue;

            bool isHost = NetworkBattleEntry.Instance != null && NetworkBattleEntry.Instance.IsHost;

            bool attackerIsPlayer = entity.Has<NetworkIdentityComponent>();
            bool attackerIsMonster = entity.Has<MonsterNetworkIdentityComponent>();

            // 非房主收到远端玩家攻击动画时，只播放表现，不结算伤害
            if (attackerIsPlayer)
            {
                var net = entity.Get<NetworkIdentityComponent>();

                if (!net.IsLocalPlayer && !isHost)
                {
                    evt.HitFrame = false;
                    entity.Add(evt);
                    continue;
                }
            }

            // 怪物伤害只允许房主结算
            if (attackerIsMonster)
            {
                var monsterNet = entity.Get<MonsterNetworkIdentityComponent>();

                if (!monsterNet.IsHostControlled || !isHost)
                {
                    evt.HitFrame = false;
                    entity.Add(evt);
                    continue;
                }
            }

            AttackStageDefinition stage;
            if (!AttackStageUtility.TryGetStage(ref attack, runtime.CurrentComboIndex, out stage))
            {
                evt.HitFrame = false;
                entity.Add(evt);
                continue;
            }

            // 获得角色位置与朝向
            Vector3 selfPos = selfTransform.Position;
            Vector3 forward = selfTransform.Rotation * Vector3.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude <= 0.0001f)
                forward = Vector3.forward;
            else
                forward.Normalize();

            foreach (var other in world.entities)
            {
                if (other == entity) continue;
                if (!other.Has<TransformComponent>() ||
                    !other.Has<CampComponent>() ||
                    !other.Has<HealthComponent>())
                    continue;

                bool targetIsPlayer = other.Has<NetworkIdentityComponent>();
                bool targetIsMonster = other.Has<MonsterNetworkIdentityComponent>();

                // 玩家只能攻击怪物
                if (attackerIsPlayer)
                {
                    if (!targetIsMonster)
                        continue;

                    var targetMonsterNet = other.Get<MonsterNetworkIdentityComponent>();
                    if (!targetMonsterNet.IsHostControlled)
                        continue;
                }
                // 怪物只能攻击玩家
                else if (attackerIsMonster)
                {
                    if (!targetIsPlayer)
                        continue;
                }
                else
                {
                    continue;
                }

                // 排除同阵营和已死亡目标
                var otherCamp = other.Get<CampComponent>();
                var otherHealth = other.Get<HealthComponent>();

                if (otherCamp.CampId == selfCamp.CampId) continue;
                if(otherHealth.IsDead) continue;

                // 获得目标位置和To目标朝向
                var targetTransform = other.Get<TransformComponent>();
                Vector3 toTarget = targetTransform.Position - selfPos;
                toTarget.y = 0f;

                // 排除未在攻击范围/距离过近 的攻击请求
                float distance = toTarget.magnitude;
                if (distance > stage.AttackRange) continue;
                if (distance <= 0.001f) continue;

                Vector3 hitDir = toTarget.normalized;

                // 计算两个向量之间的夹角
                float angle = Vector3.Angle(forward, toTarget.normalized);
                // 因为 AttackAngle 通常表示扇形区域张开的总角度（如 90°）
                // 而代码中计算的 angle 是目标方向与中心线的夹角，其范围是 [0°, 180°]。要让目标落在扇形内
                // 这个夹角必须小于等于扇形半角（即总角度的一半）
                // 若 AttackAngle = 90°，则允许的夹角最大为 45°
                // 若 AttackAngle = 120°，则允许的夹角最大为 60°
                if (angle > stage.AttackAngle * 0.5f) continue;

                AppendDamageHit(other, new DamageHitInfo
                {
                    AttackerId = entity.ID,
                    TargetId = other.ID,
                    Damage = stage.AttackDamage,

                    HitWorldPosition = targetTransform.Position + Vector3.up * 1.5f,
                    HitDirection = hitDir,

                    HitStunDuration = stage.HitStunDuration,
                    HitKnockbackSpeed = stage.HitKnockbackSpeed,
                    HitKnockbackDamping = stage.HitKnockbackDamping
                });
            }

            // 取消攻击帧
            evt.HitFrame = false;
            entity.Add(evt);
        }
    }

    private static void AppendDamageHit(Entity targetEntity, DamageHitInfo hit)
    {
        DamageBufferComponent buffer;

        // 新建伤害列表
        if (targetEntity.Has<DamageBufferComponent>())
        {
            buffer = targetEntity.Get<DamageBufferComponent>();
            if (buffer.Hits == null)
                buffer.Hits = new List<DamageHitInfo>(4);
        }
        else
        {
            buffer = new DamageBufferComponent
            {
                Hits = new List<DamageHitInfo>(4)
            };
        }

        // 将本次受击加入
        buffer.Hits.Add(hit);
        targetEntity.Add(buffer);
    }
}