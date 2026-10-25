using System.Collections.Generic;

/// <summary>
/// 统一处理背包扩容
/// 
/// 没有空槽时按一整行扩容
/// </summary>
public static class InventoryExpansionUtility
{
    // 扩容
    public static void ExpandOneRow(InventoryData inventoryData)
    {
        if (inventoryData == null)
            return;

        if (inventoryData.Slots == null)
            inventoryData.Slots = new List<InventorySlotData>();

        // 从最后一个slot后开始
        int startIndex = inventoryData.Slots.Count;

        for (int i = 0; i < InventoryGridPresenter.ExpandCountPerTime; i++)
        {
            inventoryData.Slots.Add(new InventorySlotData
            {
                SlotIndex = startIndex + i,
                IsUnlocked = true,
                Item = null
            });
        }

        inventoryData.Capacity = inventoryData.Slots.Count;
    }
}