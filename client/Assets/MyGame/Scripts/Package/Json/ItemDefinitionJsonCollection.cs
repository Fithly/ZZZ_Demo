using System;
using System.Collections.Generic;

/// <summary>
/// 包一层列表，方便 JsonUtility 读取物品配置列表
/// </summary>
[Serializable]
public class ItemDefinitionJsonCollection
{
    public List<ItemDefinitionJsonModel> Items = new List<ItemDefinitionJsonModel>();
}