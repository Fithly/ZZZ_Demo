using System;
using System.Collections.Generic;

[Serializable]
public class WeaponDefinition : ItemDefinition
{
    public int RequiredLevel;                 // 等级
    public int StartNumber;                   // 星级
    public string FunctionName;               // 效果名

    public string BaseAttributionKey;         // 基础属性
    public string AdvanceAttributionKey;      // 高级属性

    public List<ItemAttributeData> Attributes = new List<ItemAttributeData>();
}