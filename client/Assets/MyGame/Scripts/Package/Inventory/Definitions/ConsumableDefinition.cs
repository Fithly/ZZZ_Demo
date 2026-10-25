using System;
using System.Collections.Generic;

[Serializable]
public class ConsumableDefinition : ItemDefinition
{
    public string UseEffectType;           // 例如 AddHP / AddBuff / Revive
    public List<ItemAttributeData> EffectParams = new List<ItemAttributeData>();  // 效果参数

    public float Cooldown;         // 冷却
    public bool BatchUseAllowed;   // 可以批量使用
}