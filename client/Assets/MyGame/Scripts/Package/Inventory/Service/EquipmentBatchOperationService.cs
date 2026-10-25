using System;

/// <summary>
/// 一键卸下全部驱动盘
/// 
/// 拆解所有带可丢弃标记的物品
/// </summary>
public class EquipmentBatchOperationService
{
    private readonly EquipmentService equipmentService;
    private readonly InventoryService inventoryService;
    private readonly ItemDefinitionRepository repository;

    public EquipmentBatchOperationService(
        EquipmentService equipmentService,
        InventoryService inventoryService,
        ItemDefinitionRepository repository)
    {
        this.equipmentService = equipmentService ?? throw new ArgumentNullException(nameof(equipmentService));
        this.inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    // 一键卸下所有驱动盘
    public EquipmentBatchOperationResult UnequipAllDriveDiscs(GameSaveData saveData)
    {
        if (saveData == null)
            return EquipmentBatchOperationResult.Fail("SaveData is null.");

        DriveDiscSlotType[] allSlots =
        {
            DriveDiscSlotType.One,
            DriveDiscSlotType.Two,
            DriveDiscSlotType.Three,
            DriveDiscSlotType.Four,
            DriveDiscSlotType.Five,
            DriveDiscSlotType.Six
        };

        foreach (var slot in allSlots)
        {
            equipmentService.UnequipDriveDisc(saveData, slot);
        }

        return EquipmentBatchOperationResult.Ok();
    }

    // 拆解标记的可丢弃物品
    public EquipmentBatchOperationResult DismantleMarkedDiscardableItems(GameSaveData saveData)
    {
        if (saveData == null || saveData.BackpackData == null || saveData.BackpackData.Slots == null)
            return EquipmentBatchOperationResult.Fail("Backpack data invalid.");

        int removedCount = 0;

        for (int i = saveData.BackpackData.Slots.Count - 1; i >= 0; i--)
        {
            var slot = saveData.BackpackData.Slots[i];
            if (slot == null || slot.Item == null)
                continue;

            if (!slot.Item.IsMarkedDiscardable)
                continue;

            if (slot.Item.IsLocked)
                continue;

            removedCount += slot.Item.Count;
            slot.Item = null;
        }

        return EquipmentBatchOperationResult.Ok(removedCount);
    }
}