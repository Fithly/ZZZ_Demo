// 战斗输入系统（只负责把玩家输入转成攻击请求）
using UnityEngine;

public class CombatInputSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.entities)
        {
            if (!entity.Has<InputComponent>() ||
                !entity.Has<AttackComponent>() ||
                !entity.Has<AttackRequestComponent>() ||
                !entity.Has<HealthComponent>())
                continue;

            var input = entity.Get<InputComponent>();
            var health = entity.Get<HealthComponent>();
            var request = entity.Get<AttackRequestComponent>();

            // 获得受击状态
            bool isInHitStun = false;
            if (entity.Has<HitReactionStateComponent>())
            {
                var hitState = entity.Get<HitReactionStateComponent>();
                isInHitStun = hitState.IsInHitStun;
            }

            // 主UI面板打开时，清空攻击请求与攻击缓冲
            // 目的：
            // 1. 打开背包/角色面板后，不能继续接收新的攻击输入
            // 2. 防止上一帧残留的 BufferTimer 继续触发攻击
            bool isMainPanelOpen = UIManager.Instance != null && UIManager.Instance.IsAnyMainPanelOpen();

            if (isMainPanelOpen)
            {
                request.IsRequesting = false;
                request.BufferTimer = 0f;
                entity.Add(request);
                continue;
            }

            if (health.IsDead || isInHitStun)
            {
                request.IsRequesting = false;
                request.BufferTimer = 0f;
                entity.Add(request);
                continue;
            }

            // 申请攻击请求（冷却在AttackSystem中处理）
            if (input.Attack)
            {
                request.IsRequesting = true;
            }

            entity.Add(request);
        }
    }
}