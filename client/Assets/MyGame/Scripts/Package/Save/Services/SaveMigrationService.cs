using System;

/// <summary>
/// 处理：
/// 旧版本升级
/// 空对象补齐
/// 背包槽位补齐
/// 默认字段回填
/// </summary>
public static class SaveMigrationService
{
    // 迁移与修复
    public static GameSaveData MigrateAndRepair(GameSaveData saveData)
    {
        if (saveData == null)
            return CreateFreshSave();

        if (saveData.PlayerBaseData == null)
            saveData.PlayerBaseData = new PlayerBaseData();

        if (saveData.PlayerAttributeData == null)
            saveData.PlayerAttributeData = new PlayerAttributeData();

        if (saveData.PlayerProgressData == null)
            saveData.PlayerProgressData = new PlayerProgressData();

        if (saveData.BackpackData == null)
            saveData.BackpackData = new InventoryData();

        if (saveData.LuaRewardClaimRecords == null)
            saveData.LuaRewardClaimRecords = new System.Collections.Generic.List<LuaRewardClaimData>();

        if (saveData.DriveDiscEquipData == null)
            saveData.DriveDiscEquipData = new DriveDiscEquipData();

        // 补齐
        RepairBaseData(saveData);
        RepairAttributeData(saveData);
        RepairProgressData(saveData);
        RepairBackpackData(saveData);
        RepairEquippedData(saveData);
        RepairLuaRewardClaimRecords(saveData);

        // 旧版本升级
        if (saveData.SaveVersion < SaveGameConstants.CurrentSaveVersion)
        {
            UpgradeToCurrentVersion(saveData);
        }

        if (saveData.SaveVersion <= 0)
            saveData.SaveVersion = SaveGameConstants.CurrentSaveVersion;

        if (saveData.SaveTimeTicks <= 0)
            saveData.SaveTimeTicks = DateTime.UtcNow.Ticks;

        return saveData;
    }

    public static GameSaveData CreateFreshSave()
    {
        var save = GameSaveData.CreateDefault();
        save.SaveVersion = SaveGameConstants.CurrentSaveVersion;
        save.SaveTimeTicks = DateTime.UtcNow.Ticks;
        return save;
    }

    private static void UpgradeToCurrentVersion(GameSaveData saveData)
    {
        // 预留未来版本迁移逻辑
        saveData.SaveVersion = SaveGameConstants.CurrentSaveVersion;
        saveData.SaveTimeTicks = DateTime.UtcNow.Ticks;
    }

    // 防基础数据为空
    private static void RepairBaseData(GameSaveData saveData)
    {
        if (string.IsNullOrEmpty(saveData.PlayerBaseData.PlayerId))
            saveData.PlayerBaseData.PlayerId = Guid.NewGuid().ToString("N");

        if (string.IsNullOrEmpty(saveData.PlayerBaseData.PlayerName))
            saveData.PlayerBaseData.PlayerName = "Player";

        if (saveData.PlayerBaseData.Level <= 0)
            saveData.PlayerBaseData.Level = 1;

        if (saveData.PlayerBaseData.Exp < 0)
            saveData.PlayerBaseData.Exp = 0;

        if(saveData.PlayerBaseData.CurrentLevelExp < 0)
            saveData.PlayerBaseData.CurrentLevelExp = 0;

        if (saveData.PlayerBaseData.Gold < 0)
            saveData.PlayerBaseData.Gold = 0;

        if (saveData.PlayerBaseData.Diamond < 0)
            saveData.PlayerBaseData.Diamond = 0;
    }

    // 基础数值补空
    private static void RepairAttributeData(GameSaveData saveData)
    {
        if (saveData.PlayerAttributeData.BaseHP <= 0)
            saveData.PlayerAttributeData.BaseHP = 100;

        if (saveData.PlayerAttributeData.BaseAttack <= 0)
            saveData.PlayerAttributeData.BaseAttack = 10;

        if (saveData.PlayerAttributeData.BaseDefense <= 0)
            saveData.PlayerAttributeData.BaseDefense = 5;

        if (saveData.PlayerAttributeData.BaseCritRate <= 0f)
            saveData.PlayerAttributeData.BaseCritRate = 0.05f;

        if (saveData.PlayerAttributeData.BaseCritDamage <= 0f)
            saveData.PlayerAttributeData.BaseCritDamage = 0.5f;
    }

    // 游戏进度补空
    private static void RepairProgressData(GameSaveData saveData)
    {
        if (string.IsNullOrEmpty(saveData.PlayerProgressData.CurrentStageId))
            saveData.PlayerProgressData.CurrentStageId = "stage_001";

        if (saveData.PlayerProgressData.UnlockedStageIds == null)
            saveData.PlayerProgressData.UnlockedStageIds = new System.Collections.Generic.List<string>();

        if (saveData.PlayerProgressData.FinishedQuestIds == null)
            saveData.PlayerProgressData.FinishedQuestIds = new System.Collections.Generic.List<string>();
    }

    // 背包数据补空
    private static void RepairBackpackData(GameSaveData saveData)
    {
        if (saveData.BackpackData.ContainerType == InventoryContainerType.None)
            saveData.BackpackData.ContainerType = InventoryContainerType.Backpack;

        if (saveData.BackpackData.Slots == null)
            saveData.BackpackData.Slots = new System.Collections.Generic.List<InventorySlotData>();

        // 至少 45 格；并且按整行补齐；如果旧存档里槽位更多，则保留更多容量
        int currentSlotCount = saveData.BackpackData.Slots.Count;
        int rawCapacity = Math.Max(saveData.BackpackData.Capacity, currentSlotCount);
        saveData.BackpackData.Capacity = InventoryGridPresenter.GetRequiredSlotCount(rawCapacity);

        while (saveData.BackpackData.Slots.Count < saveData.BackpackData.Capacity)
        {
            int slotIndex = saveData.BackpackData.Slots.Count;
            saveData.BackpackData.Slots.Add(new InventorySlotData
            {
                SlotIndex = slotIndex,
                IsUnlocked = true,
                Item = null
            });
        }

        for (int i = 0; i < saveData.BackpackData.Slots.Count; i++)
        {
            if (saveData.BackpackData.Slots[i] == null)
            {
                saveData.BackpackData.Slots[i] = new InventorySlotData
                {
                    SlotIndex = i,
                    IsUnlocked = true,
                    Item = null
                };
            }

            saveData.BackpackData.Slots[i].SlotIndex = i;

            // 把“假空物体”修正成真正的 null 槽位
            if (IsInvalidInventoryItem(saveData.BackpackData.Slots[i].Item))
                saveData.BackpackData.Slots[i].Item = null;
        }
    }

    // 统一判断“是否为无效背包物品”
    private static bool IsInvalidInventoryItem(ItemInstanceData item)
    {
        if (item == null)
            return false;

        if (item.ItemId <= 0)
            return true;

        if (item.Count <= 0)
            return true;

        if (string.IsNullOrEmpty(item.InstanceId))
            return true;

        return false;
    }

    private static void RepairEquippedData(GameSaveData saveData)
    {
        // 如果装备槽里是“假装备数据”，直接清成 null
        if (IsInvalidInventoryItem(saveData.EquippedWeapon))
            saveData.EquippedWeapon = null;

        if (saveData.DriveDiscEquipData != null)
        {
            if (IsInvalidInventoryItem(saveData.DriveDiscEquipData.One))
                saveData.DriveDiscEquipData.One = null;

            if (IsInvalidInventoryItem(saveData.DriveDiscEquipData.Two))
                saveData.DriveDiscEquipData.Two = null;

            if (IsInvalidInventoryItem(saveData.DriveDiscEquipData.Three))
                saveData.DriveDiscEquipData.Three = null;

            if (IsInvalidInventoryItem(saveData.DriveDiscEquipData.Four))
                saveData.DriveDiscEquipData.Four = null;

            if (IsInvalidInventoryItem(saveData.DriveDiscEquipData.Five))
                saveData.DriveDiscEquipData.Five = null;

            if (IsInvalidInventoryItem(saveData.DriveDiscEquipData.Six))
                saveData.DriveDiscEquipData.Six = null;
        }

        RepairItemInstance(saveData.EquippedWeapon, true);
        RepairItemInstance(saveData.DriveDiscEquipData.One, false);
        RepairItemInstance(saveData.DriveDiscEquipData.Two, false);
        RepairItemInstance(saveData.DriveDiscEquipData.Three, false);
        RepairItemInstance(saveData.DriveDiscEquipData.Four, false);
        RepairItemInstance(saveData.DriveDiscEquipData.Five, false);
        RepairItemInstance(saveData.DriveDiscEquipData.Six, false);
    }

    private static void RepairItemInstance(ItemInstanceData item, bool isWeapon)
    {
        if (item == null)
            return;

        if (item.Level <= 0)
            item.Level = isWeapon ? 1 : 0;

        item.EnhanceLevel = item.Level;

        if (item.CurrentLevelExp < 0)
            item.CurrentLevelExp = 0;

        if (item.ExtraAttributes == null)
            item.ExtraAttributes = new System.Collections.Generic.List<ItemAttributeData>();

        if (item.RolledMinorAttributeKeys == null)
            item.RolledMinorAttributeKeys = new System.Collections.Generic.List<string>();
    }

    // 修复 Lua 奖励领取记录
    private static void RepairLuaRewardClaimRecords(GameSaveData saveData)
    {
        if (saveData == null)
            return;

        if (saveData.LuaRewardClaimRecords == null)
            saveData.LuaRewardClaimRecords = new System.Collections.Generic.List<LuaRewardClaimData>();

        for (int i = saveData.LuaRewardClaimRecords.Count - 1; i >= 0; i--)
        {
            LuaRewardClaimData record = saveData.LuaRewardClaimRecords[i];

            if (record == null || string.IsNullOrEmpty(record.RewardId))
            {
                saveData.LuaRewardClaimRecords.RemoveAt(i);
                continue;
            }

            if (record.ClaimCount < 0)
                record.ClaimCount = 0;

            if (record.LastClaimTimeTicks < 0)
                record.LastClaimTimeTicks = 0;
        }
    }
}