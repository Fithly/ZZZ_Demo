using System;
using System.Collections.Generic;

/// <summary>
/// 从总背包中筛选当前分类的物品
///
/// 支持驱动盘按套装进一步筛选
/// 
/// 这是你后面扩展各种背包筛选的中心点
/// </summary>
public class InventoryFilterService
{
    private readonly ItemDefinitionRepository itemRepository;

    public InventoryFilterService(ItemDefinitionRepository itemRepository)
    {
        this.itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
    }

    public List<InventoryDisplayItem> BuildDisplayItems(InventoryData inventoryData, InventoryFilterQuery query)
    {
        if (inventoryData == null)
            throw new ArgumentNullException(nameof(inventoryData));

        if (query == null)
            query = new InventoryFilterQuery();

        var results = new List<InventoryDisplayItem>();

        // 如果背包中槽位为空 则直接返回空数据
        if (inventoryData.Slots == null)
            return results;

        // 遍历背包槽位
        for (int i = 0; i < inventoryData.Slots.Count; i++)
        {
            var slot = inventoryData.Slots[i];
            if (slot == null || slot.Item == null)
                continue;

            var definition = itemRepository.Get(slot.Item.ItemId);
            if (definition == null)
                continue;

            if (!MatchCategory(definition, query))
                continue;

            if (!MatchDriveDiscSet(definition, query))
                continue;

            // 将筛选结果返回
            results.Add(new InventoryDisplayItem
            {
                SourceSlotIndex = i,
                SourceSlotData = slot,
                ItemInstance = slot.Item,
                ItemDefinition = definition
            });
        }

        return results;
    }

    // 返回筛选类型是否正确
    private bool MatchCategory(ItemDefinition definition, InventoryFilterQuery query)
    {
        // 根据查询目标
        switch (query.Category)
        {
            case BackpackCategoryType.None:
                return true;

            case BackpackCategoryType.Weapon:
                return definition.ItemType == ItemType.Weapon;

            case BackpackCategoryType.DriveDisc:
                return definition.ItemType == ItemType.DriveDisc;

            case BackpackCategoryType.Material:
                return definition.ItemType == ItemType.Material;

            default:
                return false;
        }
    }

    // 返回是否为正确的驱动盘套装
    private bool MatchDriveDiscSet(ItemDefinition definition, InventoryFilterQuery query)
    {
        if (!query.EnableDriveDiscSetFilter)
            return true;

        if (definition.ItemType != ItemType.DriveDisc)
            return false;

        var driveDiscDefinition = definition as DriveDiscDefinition;
        if (driveDiscDefinition == null)
            return false;

        return driveDiscDefinition.SetType == query.DriveDiscSetType;
    }
}