using System;

public class EquipmentService
{
    private readonly ItemDefinitionRepository itemRepository;
    private readonly InventoryService inventoryService;

    public EquipmentService(ItemDefinitionRepository itemRepository, InventoryService inventoryService)
    {
        this.itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
        this.inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
    }

    public EquipResult EquipFromBackpack(GameSaveData saveData, int sourceSlotIndex)
    {
        // 检查数据与索引是否正确
        ValidateSaveData(saveData);
        ValidateBackpackSlotIndex(saveData.BackpackData, sourceSlotIndex);

        var slot = saveData.BackpackData.Slots[sourceSlotIndex];
        if (slot == null || slot.Item == null)
        {
            // 返回失败结果
            return new EquipResult
            {
                Success = false,
                ErrorMessage = "Source slot is empty.",
                SourceSlotIndex = sourceSlotIndex
            };
        }

        var instance = slot.Item;                                 // 物品实例
        var definition = itemRepository.Get(instance.ItemId);     // 物品定义

        // 是武器则装备武器
        if (definition.ItemType == ItemType.Weapon)
        {
            return EquipWeapon(saveData, sourceSlotIndex);
        }

        // 是驱动盘则装备驱动盘
        if (definition.ItemType == ItemType.DriveDisc)
        {
            return EquipDriveDisc(saveData, sourceSlotIndex);
        }

        // 否则返回失败结果
        return new EquipResult
        {
            Success = false,
            ErrorMessage = $"ItemType {definition.ItemType} cannot be equipped.",
            SourceSlotIndex = sourceSlotIndex
        };
    }

    // 卸下武器
    public UnequipResult UnequipWeapon(GameSaveData saveData)
    {
        ValidateSaveData(saveData);

        if (saveData.EquippedWeapon == null)
        {
            return new UnequipResult
            {
                Success = false,
                ErrorMessage = "No weapon equipped.",
                WeaponChanged = false
            };
        }

        // 装备逻辑改为“背包保留数据”，卸下时只清空角色槽位
        saveData.EquippedWeapon = null;

        return new UnequipResult
        {
            Success = true,
            WeaponChanged = true
        };
    }

    // 卸下驱动盘
    public UnequipResult UnequipDriveDisc(GameSaveData saveData, DriveDiscSlotType slotType)
    {
        ValidateSaveData(saveData);

        var equipped = GetEquippedDriveDisc(saveData, slotType);
        if (equipped == null)
        {
            return new UnequipResult
            {
                Success = false,
                ErrorMessage = $"No drive disc equipped in slot {slotType}.",
                DriveDiscSlotType = slotType
            };
        }

        // 装备逻辑改为“背包保留数据”，卸下时只清空角色槽位
        SetEquippedDriveDisc(saveData, slotType, null);

        return new UnequipResult
        {
            Success = true,
            DriveDiscSlotType = slotType
        };
    }

    private EquipResult EquipWeapon(GameSaveData saveData, int sourceSlotIndex)
    {
        // 找到对应背包里的槽位 并获取对应对象
        var sourceSlot = saveData.BackpackData.Slots[sourceSlotIndex];
        var newWeapon = sourceSlot.Item;

        if (newWeapon == null)
        {
            return new EquipResult
            {
                Success = false,
                ErrorMessage = "Source slot is empty.",
                SourceSlotIndex = sourceSlotIndex
            };
        }

        if (newWeapon.Count != 1)
        {
            return new EquipResult
            {
                Success = false,
                ErrorMessage = "Weapon item count must be 1.",
                SourceSlotIndex = sourceSlotIndex
            };
        }

        // 如果本来就装备的是同一个实例，则直接视为成功
        if (IsSameInstance(saveData.EquippedWeapon, newWeapon))
        {
            return new EquipResult
            {
                Success = true,
                SourceSlotIndex = sourceSlotIndex,
                WeaponChanged = false
            };
        }

        // 装备时不再把物品从背包拿走，只让角色槽位引用这个背包物品实例
        saveData.EquippedWeapon = newWeapon;

        var result = new EquipResult
        {
            Success = true,
            SourceSlotIndex = sourceSlotIndex,
            WeaponChanged = true
        };
        result.ChangedSlotIndices.Add(sourceSlotIndex);
        return result;
    }

    private EquipResult EquipDriveDisc(GameSaveData saveData, int sourceSlotIndex)
    {
        var sourceSlot = saveData.BackpackData.Slots[sourceSlotIndex];
        var newDisc = sourceSlot.Item;

        if (newDisc == null)
        {
            return new EquipResult
            {
                Success = false,
                ErrorMessage = "Source slot is empty.",
                SourceSlotIndex = sourceSlotIndex
            };
        }

        if (newDisc.Count != 1)
        {
            return new EquipResult
            {
                Success = false,
                ErrorMessage = "Drive disc item count must be 1.",
                SourceSlotIndex = sourceSlotIndex
            };
        }

        // 获取驱动盘编号（枚举类型）
        var discDefinition = itemRepository.GetAs<DriveDiscDefinition>(newDisc.ItemId);
        if (discDefinition == null)
        {
            return new EquipResult
            {
                Success = false,
                ErrorMessage = $"DriveDiscDefinition not found. ItemId={newDisc.ItemId}",
                SourceSlotIndex = sourceSlotIndex
            };
        }

        var targetSlotType = discDefinition.SlotType;

        // 如果本来就装备的是同一个实例，则直接视为成功
        var oldDisc = GetEquippedDriveDisc(saveData, targetSlotType);
        if (IsSameInstance(oldDisc, newDisc))
        {
            return new EquipResult
            {
                Success = true,
                SourceSlotIndex = sourceSlotIndex,
                DriveDiscSlotType = targetSlotType
            };
        }

        // 装备时不再把物品从背包拿走，只让角色槽位引用这个背包物品实例
        SetEquippedDriveDisc(saveData, targetSlotType, newDisc);

        var result = new EquipResult
        {
            Success = true,
            SourceSlotIndex = sourceSlotIndex,
            DriveDiscSlotType = targetSlotType
        };
        result.ChangedSlotIndices.Add(sourceSlotIndex);
        return result;
    }

    private ItemInstanceData GetEquippedDriveDisc(GameSaveData saveData, DriveDiscSlotType slotType)
    {
        switch (slotType)
        {
            case DriveDiscSlotType.One: return saveData.DriveDiscEquipData.One;
            case DriveDiscSlotType.Two: return saveData.DriveDiscEquipData.Two;
            case DriveDiscSlotType.Three: return saveData.DriveDiscEquipData.Three;
            case DriveDiscSlotType.Four: return saveData.DriveDiscEquipData.Four;
            case DriveDiscSlotType.Five: return saveData.DriveDiscEquipData.Five;
            case DriveDiscSlotType.Six: return saveData.DriveDiscEquipData.Six;
            default: return null;
        }
    }

    private void SetEquippedDriveDisc(GameSaveData saveData, DriveDiscSlotType slotType, ItemInstanceData item)
    {
        switch (slotType)
        {
            case DriveDiscSlotType.One:
                saveData.DriveDiscEquipData.One = item;
                break;
            case DriveDiscSlotType.Two:
                saveData.DriveDiscEquipData.Two = item;
                break;
            case DriveDiscSlotType.Three:
                saveData.DriveDiscEquipData.Three = item;
                break;
            case DriveDiscSlotType.Four:
                saveData.DriveDiscEquipData.Four = item;
                break;
            case DriveDiscSlotType.Five:
                saveData.DriveDiscEquipData.Five = item;
                break;
            case DriveDiscSlotType.Six:
                saveData.DriveDiscEquipData.Six = item;
                break;
        }
    }

    // 统一判断“是不是同一个实例”的方法
    private bool IsSameInstance(ItemInstanceData a, ItemInstanceData b)
    {
        if (a == null || b == null)
            return false;

        if (!string.IsNullOrEmpty(a.InstanceId) && !string.IsNullOrEmpty(b.InstanceId))
            return a.InstanceId == b.InstanceId;

        // 用于判断两个引用类型变量是否指向内存中的同一个对象
        return ReferenceEquals(a, b);
    }

    private void ValidateSaveData(GameSaveData saveData)
    {
        if (saveData == null)
            throw new ArgumentNullException(nameof(saveData));

        if (saveData.BackpackData == null)
            throw new InvalidOperationException("BackpackData is null.");

        if (saveData.BackpackData.Slots == null)
            throw new InvalidOperationException("BackpackData.Slots is null.");

        if (saveData.DriveDiscEquipData == null)
            throw new InvalidOperationException("DriveDiscEquipData is null.");
    }

    private void ValidateBackpackSlotIndex(InventoryData backpackData, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= backpackData.Slots.Count)
            throw new ArgumentOutOfRangeException(nameof(slotIndex));
    }
}