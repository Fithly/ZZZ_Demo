using System;
using System.Collections.Generic;

// 背包数据
[Serializable]
public class InventoryData
{
    public InventoryContainerType ContainerType;                               // 物品类型
    public int Capacity;                                                       // 容量
    public List<InventorySlotData> Slots = new List<InventorySlotData>();      // 物品槽
}