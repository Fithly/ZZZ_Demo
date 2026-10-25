using System;
using System.Collections.Generic;

/// <summary>
/// 从总背包中筛出“当前槽位可装备”的候选物品
/// </summary>
public class EquipmentCandidateFilterService
{
    private readonly ItemDefinitionRepository repository;

    public EquipmentCandidateFilterService(ItemDefinitionRepository repository)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public List<InventoryDisplayItem> BuildCandidates(InventoryData inventoryData, EquipmentCandidateFilterQuery query)
    {
        var results = new List<InventoryDisplayItem>();
        if (inventoryData == null || inventoryData.Slots == null || query == null)
            return results;

        for (int i = 0; i < inventoryData.Slots.Count; i++)
        {
            // 检查槽
            var slot = inventoryData.Slots[i];
            if (slot == null || slot.Item == null)
                continue;

            // 检查物品
            var def = repository.Get(slot.Item.ItemId);
            if (def == null)
                continue;

            // 筛选该物品是否符合要求
            if (!CanEquipToSlot(def, query))
                continue;

            results.Add(new InventoryDisplayItem
            {
                SourceSlotIndex = i,
                SourceSlotData = slot,
                ItemInstance = slot.Item,
                ItemDefinition = def
            });
        }

        return results;
    }

    private bool CanEquipToSlot(ItemDefinition def, EquipmentCandidateFilterQuery query)
    {
        // 是武器
        if (query.SlotCategory == EquipmentSlotCategoryType.Weapon)
            return def.ItemType == ItemType.Weapon;

        // 是驱动盘
        if (query.SlotCategory == EquipmentSlotCategoryType.DriveDisc)
        {
            if (def.ItemType != ItemType.DriveDisc)
                return false;

            var discDef = def as DriveDiscDefinition;
            if (discDef == null)
                return false;

            return discDef.SlotType == query.DriveDiscSlotType;
        }

        return false;
    }
}