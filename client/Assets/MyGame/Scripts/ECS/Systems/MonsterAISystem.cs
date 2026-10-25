using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// 没目标：Idle

// 有目标且不在攻击范围内：追

// 有目标且在攻击范围内：停下并攻击
public class MonsterAISystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.entities)
        {
            if (!entity.Has<MonsterAIComponent>() ||
                !entity.Has<TransformComponent>() ||
                !entity.Has<TargetComponent>() ||
                !entity.Has<MoveIntentComponent>() ||
                !entity.Has<AttackRequestComponent>() ||
                !entity.Has<AttackComponent>() ||
                !entity.Has<AttackRuntimeComponent>() ||
                !entity.Has<HealthComponent>() ||
                !entity.Has<HitReactionStateComponent>())
                continue;

            var ai = entity.Get<MonsterAIComponent>();
            var selfTransform = entity.Get<TransformComponent>();
            var targetComp = entity.Get<TargetComponent>();
            var moveIntent = entity.Get<MoveIntentComponent>();
            var attackReq = entity.Get<AttackRequestComponent>();
            var attack = entity.Get<AttackComponent>();
            var runtime = entity.Get<AttackRuntimeComponent>();
            var health = entity.Get<HealthComponent>();
            var hitState = entity.Get<HitReactionStateComponent>();

            // 怪物死亡
            if (health.IsDead)
            {
                ai.State = MonsterAIState.Dead;
                moveIntent.MoveInput = Vector2.zero;
                attackReq.IsRequesting = false;
                selfTransform.CanMove = false;

                entity.Add(ai);
                entity.Add(moveIntent);
                entity.Add(attackReq);
                entity.Add(selfTransform);
                continue;
            }

            // 怪物受击
            if (hitState.IsInHitStun)
            {
                ai.State = MonsterAIState.Hit;
                moveIntent.MoveInput = Vector2.zero;
                attackReq.IsRequesting = false;
                selfTransform.CanMove = false;

                entity.Add(ai);
                entity.Add(moveIntent);
                entity.Add(attackReq);
                entity.Add(selfTransform);
                continue;
            }

            // 目标
            Entity targetEntity = FindTargetById(world, targetComp.TargetEntityId);
            if (targetEntity == null || !targetEntity.Has<TransformComponent>() || !targetEntity.Has<HealthComponent>())
            {
                MonsterAIIdle(entity, ai, moveIntent, attackReq, selfTransform);
                continue;
            }

            // 目标是否死亡
            var targetHealth = targetEntity.Get<HealthComponent>();
            if (targetHealth.IsDead)
            {
                MonsterAIIdle(entity, ai, moveIntent, attackReq, selfTransform);
                continue;
            }

            var targetTransform = targetEntity.Get<TransformComponent>();

            Vector3 toTarget = targetTransform.Position - selfTransform.Position;
            toTarget.y = 0f;

            // 距离过近保持攻击状态
            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                ai.State = MonsterAIState.Attack;
                moveIntent.MoveInput = Vector2.zero;
                attackReq.IsRequesting = true;
                selfTransform.CanMove = false;

                entity.Add(ai);
                entity.Add(moveIntent);
                entity.Add(attackReq);
                entity.Add(selfTransform);
                continue;
            }

            // 改变怪物朝向
            Vector3 dir = toTarget.normalized;
            selfTransform.Rotation = Quaternion.LookRotation(dir);

            int nextCombo = runtime.CurrentComboIndex <= 0 || runtime.CurrentComboIndex >= attack.MaxCombo
                ? 1 : runtime.CurrentComboIndex;

            AttackStageDefinition stage;
            // 如果没有攻击配置
            if (!AttackStageUtility.TryGetStage(ref attack, nextCombo, out stage))
            {
                stage = default;
                stage.AttackRange = 1.5f;
                stage.TargetSearchRange = 6f;
            }

            float dist = toTarget.magnitude;

            if (dist <= stage.AttackRange)
            {
                ai.State = MonsterAIState.Attack;
                moveIntent.MoveInput = Vector2.zero;
                attackReq.IsRequesting = true;
            }
            else if(dist <= stage.TargetSearchRange)
            {
                ai.State = MonsterAIState.Chase;
                // 将位移朝向自身前方
                moveIntent.MoveInput = new Vector2(0f, 1f);
                attackReq.IsRequesting = false;
            }
            else
            {
                MonsterAIIdle(entity, ai, moveIntent, attackReq, selfTransform);
                continue;
            }

            entity.Add(ai);
            entity.Add(selfTransform);
            entity.Add(moveIntent);
            entity.Add(attackReq);
        }
    }

    private void MonsterAIIdle(Entity entity, MonsterAIComponent ai, MoveIntentComponent moveIntent, 
                                AttackRequestComponent attackReq, TransformComponent selfTransform)
    {
        ai.State = MonsterAIState.Idle;
        moveIntent.MoveInput = Vector2.zero;
        attackReq.IsRequesting = false;
        selfTransform.CanMove = true;

        entity.Add(ai);
        entity.Add(selfTransform);
        entity.Add(moveIntent);
        entity.Add(attackReq);
    }

    private static Entity FindTargetById(World world, int id)
    {
        if (id < 0)
            return null;

        foreach (var entity in world.entities)
        {
            if (entity.ID == id)
                return entity;
        }

        return null;
    }
}
