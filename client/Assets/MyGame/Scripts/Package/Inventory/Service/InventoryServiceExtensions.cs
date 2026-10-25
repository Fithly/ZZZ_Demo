/// <summary>
/// 给现有 InventoryService 补一个“自动扩容新增物品”的辅助能力
/// </summary>
public static class InventoryServiceExtensions
{
    public static bool AddItemWithAutoExpand(
        this InventoryService inventoryService,
        InventoryData inventoryData,
        int itemId,
        int count)
    {
        if (inventoryService == null || inventoryData == null)
            return false;

        // 添加成功则返回
        InventoryAddResult added = inventoryService.AddItem(inventoryData, itemId, count);
        if (added.Success)
            return true;

        // ===== 修改：如果一行扩容后仍然放不下，则继续按行扩容直到放下为止 =====
        int remainCount = added.RemainingCount;

        while (remainCount > 0)
        {
            // 使用工具类进行扩容一行
            InventoryExpansionUtility.ExpandOneRow(inventoryData);
            // 添加剩余物品进去（自动添加进空槽）
            InventoryAddResult retry = inventoryService.AddItem(inventoryData, itemId, remainCount);

            if (retry.Success)
                return true;

            // 防止异常情况下进入死循环：扩容重试后剩余数量没有减少，直接失败
            if (retry.RemainingCount >= remainCount)
                return false;

            remainCount = retry.RemainingCount;
        }

        return true;
    }
}