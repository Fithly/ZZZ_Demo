using UnityEngine;

/// <summary>
/// UI 面板输入系统
/// 
/// 只负责：
/// 读取 ECS 输入
/// 调用 UIManager 打开 / 关闭面板
/// 
/// 不负责具体界面显示细节
/// </summary>
public class UIPanelInputSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        if (UIManager.Instance == null)
            return;

        foreach (var entity in world.entities)
        {
            if (!entity.Has<InputComponent>() ||
                !entity.Has<HealthComponent>())
                continue;

            var input = entity.Get<InputComponent>();
            var health = entity.Get<HealthComponent>();

            // 死亡时不响应面板输入
            if (health.IsDead)
                continue;

            // 打开/关闭背包面板
            if (input.BackPackPanel)
            {
                if (UIManager.Instance.IsAnyMainPanelOpen())
                    UIManager.Instance.CloseAllPages();
                else
                    UIManager.Instance.OpenBackpack();
            }

            // 打开/关闭角色面板
            if (input.CharactorPanel)
            {
                if (UIManager.Instance.IsAnyMainPanelOpen())
                    UIManager.Instance.CloseAllPages();
                else
                    UIManager.Instance.OpenCharacter();
            }
        }
    }
}