using UnityEngine;

// 索敌系统（寻找最近目标）
public class TargetSearchSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.entities)
        {
            if (!entity.Has<TransformComponent>() ||
                !entity.Has<CampComponent>() ||
                !entity.Has<TargetComponent>() ||
                !entity.Has<TargetingComponent>() ||
                !entity.Has<HealthComponent>())
                continue;

            var selfTransform = entity.Get<TransformComponent>();
            var selfCamp = entity.Get<CampComponent>();
            var targetComp = entity.Get<TargetComponent>();
            var targeting = entity.Get<TargetingComponent>();
            var selfHealth = entity.Get<HealthComponent>();

            // 如果死亡则取消目标
            if (selfHealth.IsDead)
            {
                targetComp.TargetEntityId = -1;
                entity.Add(targetComp);
                continue;
            }

            if (IsCurrentTargetStillValid(world, entity, selfTransform, selfCamp, targeting, targetComp.TargetEntityId))
            {
                entity.Add(targetComp);
                continue;
            }

            float minDist = float.MaxValue;
            int nearestId = -1;

            foreach (var other in world.entities)
            {
                if (other == entity) continue;
                if (!other.Has<TransformComponent>() || 
                    !other.Has<CampComponent>() || 
                    !other.Has<HealthComponent>())
                    continue;

                var otherCamp = other.Get<CampComponent>();
                var otherHealth = other.Get<HealthComponent>();

                // 排除同阵营
                if (otherCamp.CampId == selfCamp.CampId) continue;
                // 排除死亡单位
                if (otherHealth.IsDead) continue;

                // 寻找距离最近的目标
                var otherTransform = other.Get<TransformComponent>();
                float dist = Vector3.Distance(selfTransform.Position, otherTransform.Position);

                // 必须在检测范围内
                if(dist > targeting.SearchRange) continue;

                if (dist < minDist)
                {
                    minDist = dist;
                    nearestId = other.ID;
                }
            }

            // 将目标id写入目标组件
            targetComp.TargetEntityId = nearestId;
            entity.Add(targetComp);
        }
    }

    // 如果当前目标还有效且还在搜索范围内，就继续保留，避免频繁抖动切目标
    private static bool IsCurrentTargetStillValid(World world, Entity self, TransformComponent selfTransform,
                                                    CampComponent selfCamp, TargetingComponent targeting, int targetId)
    {
        if (targetId < 0)
            return false;

        foreach (var other in world.entities)
        {
            if (other == self || other.ID != targetId)
                continue;

            if (!other.Has<TransformComponent>() ||
                !other.Has<CampComponent>() ||
                !other.Has<HealthComponent>())
                return false;

            var otherCamp = other.Get<CampComponent>();
            var otherHealth = other.Get<HealthComponent>();

            if (otherCamp.CampId == selfCamp.CampId)
                return false;

            if (otherHealth.IsDead)
                return false;

            var otherTransform = other.Get<TransformComponent>();
            float dist = Vector3.Distance(selfTransform.Position, otherTransform.Position);

            return dist <= targeting.SearchRange;
        }

        return false;
    }
}