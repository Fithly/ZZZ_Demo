using System;
using System.Collections.Generic;

/// <summary>
/// 材料配置
/// 对应 material_items.json
/// </summary>
[Serializable]
public class MaterialDefinitionConfig : ItemConfigCommon
{
    // 保留用途类型，便于配置校验、筛选扩展
    public MaterialPurposeType PurposeType;

    // 预留：后面扩展材料功能时可继续增加字段
    public List<ItemAttributeConfig> Attributes = new List<ItemAttributeConfig>();
}

/// <summary>
/// 材料配置文件包装
/// </summary>
[Serializable]
public class MaterialDefinitionConfigList
{
    public List<MaterialDefinitionConfig> Items = new List<MaterialDefinitionConfig>();
}