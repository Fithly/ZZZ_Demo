using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 处理伤害队列：
/// 一帧内可以有多条命中，逐条扣血，逐条弹白字
/// 如果目标未死亡，则追加受击请求
/// </summary>
public class DamageSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.entities)
        {
            // 查找是否存在伤害组件
            if (!entity.Has<DamageBufferComponent>() || !entity.Has<HealthComponent>())
                continue;

            var buffer = entity.Get<DamageBufferComponent>();
            var health = entity.Get<HealthComponent>();

            if (buffer.Hits == null || buffer.Hits.Count == 0)
            {
                entity.Remove<DamageBufferComponent>();
                continue;
            }

            // 遍历伤害组件
            for (int i = 0; i < buffer.Hits.Count; i++)
            {
                var hit = buffer.Hits[i];

                if (hit.Damage <= 0)
                    continue;

                // 如果目标已经死了，就不再继续吃后续伤害
                if (health.IsDead)
                    break;

                health.CurrentHP -= hit.Damage;
                if (health.CurrentHP <= 0)
                {
                    health.CurrentHP = 0;
                    health.IsDead = true;
                }

                // 生成伤害数字
                DamageNumberSpawner.Spawn(hit.Damage, hit.HitWorldPosition);

                // 修改：目标可能是怪物，也可能是玩家，都要广播
                BroadcastNetworkDamage(world, entity, hit, health);

                if (!health.IsDead)
                {
                    AppendHitReactionRequest(entity, new HitReactionRequestInfo
                    {
                        HitDirection = NormalizePlanar(hit.HitDirection),
                        StunDuration = hit.HitStunDuration,
                        KnockbackSpeed = hit.HitKnockbackSpeed,
                        KnockbackDamping = hit.HitKnockbackDamping
                    });
                }
            }

            entity.Add(health);

            buffer.Hits.Clear();
            entity.Add(buffer);
            entity.Remove<DamageBufferComponent>();
        }
    }

    private static Vector3 NormalizePlanar(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.0001f)
            return Vector3.zero;

        return dir.normalized;
    }

    private static void AppendHitReactionRequest(Entity entity, HitReactionRequestInfo request)
    {
        HitReactionBufferComponent buffer;

        // 新建受击列表
        if (entity.Has<HitReactionBufferComponent>())
        {
            buffer = entity.Get<HitReactionBufferComponent>();
            if (buffer.Requests == null)
                buffer.Requests = new List<HitReactionRequestInfo>(4);
        }
        else
        {
            buffer = new HitReactionBufferComponent
            {
                Requests = new List<HitReactionRequestInfo>(4)
            };
        }

        // 添加本次受击请求
        buffer.Requests.Add(request);
        entity.Add(buffer);
    }

    // 广播网络伤害，目标可以是怪物，也可以是玩家
    private static void BroadcastNetworkDamage(World world, Entity targetEntity, DamageHitInfo hit, HealthComponent targetHealth)
    {
        if (NetworkBattleEntry.Instance == null)
            return;

        if (!NetworkBattleEntry.Instance.IsHost)
            return;

        Entity attackerEntity = world.GetEntityById(hit.AttackerId);
        if (attackerEntity == null)
            return;

        string attackerId = GetNetworkId(attackerEntity);
        string targetId = GetNetworkId(targetEntity);

        if (string.IsNullOrEmpty(attackerId) || string.IsNullOrEmpty(targetId))
            return;

        // 发送受击信息包
        NetworkBattleEntry.Instance.SendBattleHit(
            attackerId,
            targetId,
            hit.Damage,
            targetHealth.CurrentHP,
            targetHealth.MaxHP,
            hit.HitWorldPosition,
            hit.HitDirection);

        // 发送死亡信息包
        if (targetHealth.IsDead)
        {
            Vector3 deathPos = hit.HitWorldPosition;
            float rotY = 0f;

            if (targetEntity.Has<TransformComponent>())
            {
                var trans = targetEntity.Get<TransformComponent>();
                deathPos = trans.Position;
                rotY = trans.Rotation.eulerAngles.y;
            }

            NetworkBattleEntry.Instance.SendBattleDeath(
                attackerId,
                targetId,
                deathPos,
                rotY);
        }
    }

    private static string GetNetworkId(Entity entity)
    {
        if (entity == null)
            return string.Empty;

        if (entity.Has<NetworkIdentityComponent>())
        {
            var net = entity.Get<NetworkIdentityComponent>();
            return net.PlayerId;
        }

        if (entity.Has<MonsterNetworkIdentityComponent>())
        {
            var monsterNet = entity.Get<MonsterNetworkIdentityComponent>();
            return monsterNet.MonsterId;
        }

        return string.Empty;
    }
}
