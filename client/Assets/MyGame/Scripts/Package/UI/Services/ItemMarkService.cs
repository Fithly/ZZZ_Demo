/// <summary>
/// 管理物品标记
/// 
/// 切换锁定 / 可丢弃
/// </summary>
public class ItemMarkService
{
    public void ToggleLock(ItemInstanceData item)
    {
        if (item == null)
            return;

        item.IsLocked = !item.IsLocked;
    }

    public void ToggleDiscardable(ItemInstanceData item)
    {
        if (item == null)
            return;

        item.IsMarkedDiscardable = !item.IsMarkedDiscardable;
    }
}