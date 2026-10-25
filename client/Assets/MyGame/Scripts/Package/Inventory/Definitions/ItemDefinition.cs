using System;
using System.Collections.Generic;

[Serializable]
public class ItemDefinition
{
    public int ItemId;
    public string Name;
    public string Description;

    public ItemType ItemType;         // 物品大类
    //public ItemSubType ItemSubType;   // 物品小类
    public ItemQuality Quality;       // 物品品质

    public string IconKey;            // 图标资源路径
    public int MaxStack;              // 最大堆叠
    public int SellPrice;             // 出售价格
    public bool CanStack;             // 可堆叠
}