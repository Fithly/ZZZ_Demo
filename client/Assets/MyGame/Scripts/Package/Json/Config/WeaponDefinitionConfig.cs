using System;
using System.Collections.Generic;

/// <summary>
/// 武器配置
/// 对应 weapon_items.json
/// </summary>
[Serializable]
public class WeaponDefinitionConfig : ItemConfigCommon
{
    public int StartNumber;
    public string FunctionName;
    public string BaseAttributionKey;
    public string AdvanceAttributionKey;

    // 定义层基础属性
    public List<ItemAttributeConfig> Attributes = new List<ItemAttributeConfig>();
}

/// <summary>
/// 武器配置文件包装
/// 让 JsonUtility 可以直接反序列化
/// </summary>
[Serializable]
public class WeaponDefinitionConfigList
{
    public List<WeaponDefinitionConfig> Items = new List<WeaponDefinitionConfig>();
}

/// <summary>
/// 属性配置
/// Json 配置层通用
/// </summary>
[Serializable]
public class ItemAttributeConfig
{
    public string Key;
    public float Value;
}