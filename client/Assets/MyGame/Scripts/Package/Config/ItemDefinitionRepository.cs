using System;
using System.Collections.Generic;

/// <summary>
/// 物品定义仓库
/// 负责：
/// 根据 ItemId 存取定义
/// 批量注册定义
/// 校验重复 ItemId
/// </summary>
public class ItemDefinitionRepository
{
    // 根据ID存物品信息
    private readonly Dictionary<int, ItemDefinition> definitions = new Dictionary<int, ItemDefinition>();

    public int Count => definitions.Count;

    // 清空
    public void Clear()
    {
        definitions.Clear();
    }

    // 注册物品
    public void Register(ItemDefinition definition)
    {
        if (definition == null)
            return;

        // 对非法 ItemId 做保护
        if (definition.ItemId <= 0)
            throw new ArgumentException($"Invalid ItemId: {definition.ItemId}");

        // 对重复 ItemId 做显式校验，避免配置表静默覆盖
        if (definitions.ContainsKey(definition.ItemId))
            throw new InvalidOperationException($"Duplicate ItemId detected: {definition.ItemId}");

        definitions.Add(definition.ItemId, definition);
    }

    // 将列表物品注册
    public void RegisterRange(List<ItemDefinition> definitionList)
    {
        if (definitionList == null)
            return;

        Clear();

        for (int i = 0; i < definitionList.Count; i++)
        {
            Register(definitionList[i]);
        }
    }

    // 根据ID看是否得到物品
    public bool TryGet(int itemId, out ItemDefinition definition)
    {
        return definitions.TryGetValue(itemId, out definition);
    }

    // 获取物品
    public ItemDefinition Get(int itemId)
    {
        definitions.TryGetValue(itemId, out var definition);
        return definition;
    }

    // 泛型具体哪种物品
    public T GetAs<T>(int itemId) where T : ItemDefinition
    {
        var definition = Get(itemId);

        if (definition is T typed)
            return typed;

        throw new InvalidCastException(
            $"ItemDefinition {itemId} is of type {definition.GetType().Name}, not {typeof(T).Name}");
    }

    // 是否存在
    public bool Contains(int itemId)
    {
        return definitions.ContainsKey(itemId);
    }

    // 返回列表
    public List<ItemDefinition> GetAll()
    {
        return new List<ItemDefinition>(definitions.Values);
    }

    // 返回相同类型的物品
    public List<ItemDefinition> GetByItemType(ItemType itemType)
    {
        var result = new List<ItemDefinition>();

        foreach (var pair in definitions)
        {
            if (pair.Value.ItemType == itemType)
                result.Add(pair.Value);
        }

        return result;
    }

    //// 返回具体类型的物品列表
    //public List<ItemDefinition> GetBySubType(ItemSubType itemSubType)
    //{
    //    var result = new List<ItemDefinition>();

    //    foreach (var pair in definitions)
    //    {
    //        if (pair.Value.ItemSubType == itemSubType)
    //            result.Add(pair.Value);
    //    }

    //    return result;
    //}
}