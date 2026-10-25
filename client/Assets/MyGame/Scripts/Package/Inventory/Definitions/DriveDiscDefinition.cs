using System;
using System.Collections.Generic;

[Serializable]
public class DriveDiscDefinition : ItemDefinition
{
    public DriveDiscSlotType SlotType;        // 1~6号位
    public DriveDiscSetType SetType;          // 驱动盘种类/套装类型
    public int DiscNumber;                    // 编号

    public int RequiredLevel;                 // 等级
    public string FunctionName;               // 效果名
    public string AllowedCharacterTag;

    public string MainAttributionKey;         // 主属性
    public List<string> MinorAttributionKey = new List<string>(4);      // 副属性

    public List<ItemAttributeData> Attributes = new List<ItemAttributeData>();  // 物品基础属性数值
}