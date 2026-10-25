using System;

[Serializable]
public class InventorySlotData
{
    public int SlotIndex;               // 槽索引
    public bool IsUnlocked;             // 是否锁定
    public ItemInstanceData Item;       // 物品信息
}