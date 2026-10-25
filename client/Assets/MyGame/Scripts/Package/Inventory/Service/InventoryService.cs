using System;
using System.Collections.Generic;
using System.Diagnostics;

public class InventoryService
{
    private readonly ItemDefinitionRepository itemRepository;

    public InventoryService(ItemDefinitionRepository itemRepository)
    {
        // 防止参数为空 为空则抛出异常 指出参数名
        this.itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
    }

    // 获取物品数量
    public InventoryQueryResult GetItemCount(InventoryData inventoryData, int itemId)
    { 
        // 检验数据是否存在且是否为空
        ValidateInventory(inventoryData);

        int totalCount = 0;

        foreach (var slot in inventoryData.Slots)
        {
            if (slot == null || slot.Item == null)
                continue;

            // 防止有多栏
            if (slot.Item.ItemId == itemId)
                totalCount += slot.Item.Count;
        }

        // 返回查询结果
        return new InventoryQueryResult
        {
            ItemId = itemId,
            TotalCount = totalCount
        };
    }

    // 物品数量是否满足
    public bool HasEnoughItem(InventoryData inventoryData, int itemId, int needCount)
    {
        if (needCount <= 0)
            return true;

        return GetItemCount(inventoryData, itemId).TotalCount >= needCount;
    }

    // 增添物品
    public InventoryAddResult AddItem(InventoryData inventoryData, int itemId, int count)
    {
        ValidateInventory(inventoryData);

        if (count <= 0)
            throw new ArgumentException("Add count must be > 0");

        // 通过物品库得到物品
        var definition = itemRepository.Get(itemId);

        // 物品添加结果信息
        var result = new InventoryAddResult
        {
            Success = false,
            ItemId = itemId,
            RequestedCount = count,
            AddedCount = 0,
            RemainingCount = count
        };

        if (definition.CanStack)
        {
            // 自动堆叠
            AddToExistingStacks(inventoryData, definition, ref count, result);
        }

        // 如果堆叠完之后多出最大堆叠数量
        if (count > 0)
        {
            // 添加进空槽
            AddToEmptySlots(inventoryData, definition, ref count, result);
        }

        result.AddedCount = result.RequestedCount - count;
        result.RemainingCount = count;
        result.Success = count == 0;

        return result;
    }

    // 移除物品
    public InventoryRemoveResult RemoveItem(InventoryData inventoryData, int itemId, int count)
    {
        ValidateInventory(inventoryData);

        if (count <= 0)
            throw new ArgumentException("Remove count must be > 0");

        var result = new InventoryRemoveResult
        {
            Success = false,
            ItemId = itemId,
            RequestedCount = count,
            RemovedCount = 0,
            RemainingNeedCount = count
        };

        for (int i = 0; i < inventoryData.Slots.Count; i++)
        {
            var slot = inventoryData.Slots[i];

            if (slot == null || slot.Item == null)
                continue;

            if (slot.Item.ItemId != itemId)
                continue;

            // 移除最小值 避免成为负数
            int removable = Math.Min(slot.Item.Count, count);
            slot.Item.Count -= removable;
            count -= removable;

            if (!result.ChangedSlotIndices.Contains(i))
                result.ChangedSlotIndices.Add(i);

            if (slot.Item.Count <= 0)
                slot.Item = null;

            if (count <= 0)
                break;
        }

        result.RemovedCount = result.RequestedCount - count;  // 实际移除了多少数目
        result.RemainingNeedCount = count;                    // 剩余需求数量
        result.Success = count == 0;                          // 是否执行成功

        return result;
    }

    // 找到第一个空槽索引
    public int FindFirstEmptySlotIndex(InventoryData inventoryData)
    {
        ValidateInventory(inventoryData);

        for (int i = 0; i < inventoryData.Slots.Count; i++)
        {
            var slot = inventoryData.Slots[i];

            if (slot == null)
                continue;

            if (!slot.IsUnlocked)
                continue;

            // ===== 修改：无效物品实例也视为空槽 =====
            if (IsEmptyItem(slot.Item))
            {
                slot.Item = null;
                return i;
            }
        }

        return -1;
    }

    // 根据物品ID找到槽索引
    public List<int> FindSlotsByItemId(InventoryData inventoryData, int itemId)
    {
        ValidateInventory(inventoryData);

        var result = new List<int>();

        for (int i = 0; i < inventoryData.Slots.Count; i++)
        {
            var slot = inventoryData.Slots[i];

            if (slot == null || slot.Item == null)
                continue;

            if (slot.Item.ItemId == itemId)
                result.Add(i);
        }

        return result;
    }

    // 自动物品堆叠
    private void AddToExistingStacks(
        InventoryData inventoryData,
        ItemDefinition definition,
        ref int remainCount,
        InventoryAddResult result)
    {
        for (int i = 0; i < inventoryData.Slots.Count; i++)
        {
            if (remainCount <= 0)
                return;

            var slot = inventoryData.Slots[i];

            // 槽位不为空
            if (slot == null || slot.Item == null)
                continue;

            // 槽位Id对应
            if (slot.Item.ItemId != definition.ItemId)
                continue;

            // 不可大于最大堆叠数
            int canAdd = definition.MaxStack - slot.Item.Count;
            if (canAdd <= 0)
                continue;

            int addCount = Math.Min(canAdd, remainCount);
            slot.Item.Count += addCount;
            // 以免超出堆叠数
            remainCount -= addCount;

            // 更改的槽位索引
            if (!result.ChangedSlotIndices.Contains(i))
                result.ChangedSlotIndices.Add(i);
        }
    }

    private void AddToEmptySlots(
        InventoryData inventoryData,
        ItemDefinition definition,
        ref int remainCount,
        InventoryAddResult result)
    {
        for (int i = 0; i < inventoryData.Slots.Count; i++)
        {
            if (remainCount <= 0)
                return;

            var slot = inventoryData.Slots[i];

            if (slot == null)
                continue;

            if (!slot.IsUnlocked)
                continue;

            // 无效物品实例也视为空槽，并顺手置空
            if (!IsEmptyItem(slot.Item))
                continue;

            slot.Item = null;

            int addCount = definition.CanStack
                ? Math.Min(definition.MaxStack, remainCount)
                : 1;

            slot.Item = CreateItemInstance(definition, addCount);
            remainCount -= addCount;

            if (!result.ChangedSlotIndices.Contains(i))
                result.ChangedSlotIndices.Add(i);

            // 如果不可堆叠
            if (!definition.CanStack && remainCount > 0)
            {
                while (remainCount > 0)
                {
                    // 找到下一个空槽索引
                    int nextEmpty = FindNextEmptySlotIndex(inventoryData, i + 1);
                    if (nextEmpty < 0)
                        return;

                    inventoryData.Slots[nextEmpty].Item = CreateItemInstance(definition, 1);
                    remainCount -= 1;

                    if (!result.ChangedSlotIndices.Contains(nextEmpty))
                        result.ChangedSlotIndices.Add(nextEmpty);
                }
            }
        }
    }

    private int FindNextEmptySlotIndex(InventoryData inventoryData, int startIndex)
    {
        for (int i = startIndex; i < inventoryData.Slots.Count; i++)
        {
            var slot = inventoryData.Slots[i];

            if (slot == null)
                continue;

            if (!slot.IsUnlocked)
                continue;

            // 无效物品实例也视为空槽，并顺手置空
            if (IsEmptyItem(slot.Item))
            {
                slot.Item = null;
                return i;
            }
        }

        return -1;
    }

    private ItemInstanceData CreateItemInstance(ItemDefinition definition, int count)
    {
        int level = 0;

        if (definition != null)
        {
            if (definition.ItemType == ItemType.Weapon)
                level = 1;
            else if (definition.ItemType == ItemType.DriveDisc)
                level = 0;
        }

        return new ItemInstanceData
        {
            InstanceId = Guid.NewGuid().ToString("N"),
            ItemId = definition != null ? definition.ItemId : 0,
            Count = count,
            IsBound = false,
            EnhanceLevel = level,
            Level = level,
            CurrentLevelExp = 0,
            Durability = 0,
            AcquireSourceType = ItemAcquireSourceType.None,
            AcquireSourceParam = string.Empty,
            CreateTimeTicks = DateTime.UtcNow.Ticks
        };
    }

    // 统一判断“是否为空槽物品”的方法
    private bool IsEmptyItem(ItemInstanceData item)
    {
        if (item == null)
            return true;

        if (item.ItemId <= 0)
            return true;

        if (item.Count <= 0)
            return true;

        if (string.IsNullOrEmpty(item.InstanceId))
            return true;

        return false;
    }

    private void ValidateInventory(InventoryData inventoryData)
    {
        if (inventoryData == null)
            throw new ArgumentNullException(nameof(inventoryData));

        if (inventoryData.Slots == null)
            throw new InvalidOperationException("InventoryData.Slots is null");
    }
}