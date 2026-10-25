using System;
using System.Collections.Generic;

/// <summary>
/// 驱动盘配置
/// 对应 drive_disc_items.json
/// </summary>
[Serializable]
public class DriveDiscDefinitionConfig : ItemConfigCommon
{
    public DriveDiscSetType SetType;
    public DriveDiscSlotType SlotType;
    public int DiscNumber;

    public string FunctionName;
    public string MainAttributionKey;

    // 四个副属性 key
    public List<string> MinorAttributionKey = new List<string>();

    // 定义层基础属性
    public List<ItemAttributeConfig> Attributes = new List<ItemAttributeConfig>();
}

/// <summary>
/// 驱动盘配置文件包装
/// </summary>
[Serializable]
public class DriveDiscDefinitionConfigList
{
    public List<DriveDiscDefinitionConfig> Items = new List<DriveDiscDefinitionConfig>();
}