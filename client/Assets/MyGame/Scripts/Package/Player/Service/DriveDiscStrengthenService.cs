using System;
using System.Collections.Generic;

/// <summary>
/// 驱动盘等级升级服务
/// </summary>
public class DriveDiscStrengthenService
{
    private readonly InventoryService inventoryService;
    private readonly Random random = new Random();       // 随机数

    public DriveDiscStrengthenService(InventoryService inventoryService)
    {
        this.inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
    }

    // 预览
    public StrengthenPreviewResult Preview(GameSaveData saveData, ItemInstanceData driveDiscInstance, DriveDiscDefinition definition, int lowCount, int midCount, int highCount)
    {
        if (saveData == null || saveData.BackpackData == null)
            return StrengthenPreviewResult.Fail("SaveData invalid.");

        if (driveDiscInstance == null || definition == null)
            return StrengthenPreviewResult.Fail("Drive disc target invalid.");

        // 拥有数量
        int ownedLow = inventoryService.GetItemCount(saveData.BackpackData, StrengthenMaterialIds.DriveDiscExpLow).TotalCount;
        int ownedMid = inventoryService.GetItemCount(saveData.BackpackData, StrengthenMaterialIds.DriveDiscExpMid).TotalCount;
        int ownedHigh = inventoryService.GetItemCount(saveData.BackpackData, StrengthenMaterialIds.DriveDiscExpHigh).TotalCount;

        if (lowCount < 0 || midCount < 0 || highCount < 0)
            return StrengthenPreviewResult.Fail("Selected material count cannot be negative.");

        if (lowCount > ownedLow || midCount > ownedMid || highCount > ownedHigh)
            return StrengthenPreviewResult.Fail("Selected material count exceeds owned amount.");

        // 添加的经验
        int addedExp =
            lowCount * StrengthenBalanceConfig.DriveDiscExpLowValue +
            midCount * StrengthenBalanceConfig.DriveDiscExpMidValue +
            highCount * StrengthenBalanceConfig.DriveDiscExpHighValue;

        int level = driveDiscInstance.Level;
        int exp = driveDiscInstance.CurrentLevelExp;

        // 模拟升级
        SimulateLevelUp(
            ref level,
            ref exp,
            addedExp,
            StrengthenBalanceConfig.DriveDiscMaxLevel,
            StrengthenBalanceConfig.GetDriveDiscNeedExp);

        var result = StrengthenPreviewResult.Ok();
        result.CurrentLevel = driveDiscInstance.Level;
        result.CurrentLevelExp = driveDiscInstance.CurrentLevelExp;
        result.PreviewLevel = level;
        result.PreviewLevelExp = exp;
        result.AddedExp = addedExp;
        result.GainedLevelCount = Math.Max(0, level - driveDiscInstance.Level);
        result.ReachedMaxLevel = level >= StrengthenBalanceConfig.DriveDiscMaxLevel;

        result.MaterialSelections.Add(new StrengthenMaterialSelection
        {
            ItemId = StrengthenMaterialIds.DriveDiscExpLow,
            OwnedCount = ownedLow,
            SelectedCount = lowCount,
            ExpPerItem = StrengthenBalanceConfig.DriveDiscExpLowValue
        });

        result.MaterialSelections.Add(new StrengthenMaterialSelection
        {
            ItemId = StrengthenMaterialIds.DriveDiscExpMid,
            OwnedCount = ownedMid,
            SelectedCount = midCount,
            ExpPerItem = StrengthenBalanceConfig.DriveDiscExpMidValue
        });

        result.MaterialSelections.Add(new StrengthenMaterialSelection
        {
            ItemId = StrengthenMaterialIds.DriveDiscExpHigh,
            OwnedCount = ownedHigh,
            SelectedCount = highCount,
            ExpPerItem = StrengthenBalanceConfig.DriveDiscExpHighValue
        });

        return result;
    }

    public StrengthenPreviewResult BuildAutoPlan(GameSaveData saveData, ItemInstanceData driveDiscInstance, DriveDiscDefinition definition)
    {
        if (saveData == null || driveDiscInstance == null || definition == null)
            return StrengthenPreviewResult.Fail("Drive disc target invalid.");

        int lowOwned = inventoryService.GetItemCount(saveData.BackpackData, StrengthenMaterialIds.DriveDiscExpLow).TotalCount;
        int midOwned = inventoryService.GetItemCount(saveData.BackpackData, StrengthenMaterialIds.DriveDiscExpMid).TotalCount;
        int highOwned = inventoryService.GetItemCount(saveData.BackpackData, StrengthenMaterialIds.DriveDiscExpHigh).TotalCount;

        int low = 0;
        int mid = 0;
        int high = 0;

        int level = driveDiscInstance.Level;
        int exp = driveDiscInstance.CurrentLevelExp;

        while (level < StrengthenBalanceConfig.DriveDiscMaxLevel)
        {
            int need = StrengthenBalanceConfig.GetDriveDiscNeedExp(level) - exp;
            if (need <= 0)
            {
                level++;
                exp = 0;
                continue;
            }

            if (high < highOwned && StrengthenBalanceConfig.DriveDiscExpHighValue <= need * 3)
            {
                high++;
                AddExpOnce(ref level, ref exp, StrengthenBalanceConfig.DriveDiscExpHighValue,
                    StrengthenBalanceConfig.DriveDiscMaxLevel, StrengthenBalanceConfig.GetDriveDiscNeedExp);
                continue;
            }

            if (mid < midOwned && StrengthenBalanceConfig.DriveDiscExpMidValue <= need * 2)
            {
                mid++;
                AddExpOnce(ref level, ref exp, StrengthenBalanceConfig.DriveDiscExpMidValue,
                    StrengthenBalanceConfig.DriveDiscMaxLevel, StrengthenBalanceConfig.GetDriveDiscNeedExp);
                continue;
            }

            if (low < lowOwned)
            {
                low++;
                AddExpOnce(ref level, ref exp, StrengthenBalanceConfig.DriveDiscExpLowValue,
                    StrengthenBalanceConfig.DriveDiscMaxLevel, StrengthenBalanceConfig.GetDriveDiscNeedExp);
                continue;
            }

            if (mid < midOwned)
            {
                mid++;
                AddExpOnce(ref level, ref exp, StrengthenBalanceConfig.DriveDiscExpMidValue,
                    StrengthenBalanceConfig.DriveDiscMaxLevel, StrengthenBalanceConfig.GetDriveDiscNeedExp);
                continue;
            }

            if (high < highOwned)
            {
                high++;
                AddExpOnce(ref level, ref exp, StrengthenBalanceConfig.DriveDiscExpHighValue,
                    StrengthenBalanceConfig.DriveDiscMaxLevel, StrengthenBalanceConfig.GetDriveDiscNeedExp);
                continue;
            }

            break;
        }

        return Preview(saveData, driveDiscInstance, definition, low, mid, high);
    }

    public StrengthenApplyResult Apply(GameSaveData saveData, ItemInstanceData driveDiscInstance, DriveDiscDefinition definition, int lowCount, int midCount, int highCount)
    {
        var preview = Preview(saveData, driveDiscInstance, definition, lowCount, midCount, highCount);
        if (!preview.Success)
            return StrengthenApplyResult.Fail(preview.ErrorMessage);

        if (preview.AddedExp <= 0)
            return StrengthenApplyResult.Fail("No exp added.");

        int oldLevel = driveDiscInstance.Level;
        int oldExp = driveDiscInstance.CurrentLevelExp;

        if (lowCount > 0)
        {
            var remove = inventoryService.RemoveItem(saveData.BackpackData, StrengthenMaterialIds.DriveDiscExpLow, lowCount);
            if (!remove.Success)
                return StrengthenApplyResult.Fail("Failed to remove low drive disc exp material.");
        }

        if (midCount > 0)
        {
            var remove = inventoryService.RemoveItem(saveData.BackpackData, StrengthenMaterialIds.DriveDiscExpMid, midCount);
            if (!remove.Success)
                return StrengthenApplyResult.Fail("Failed to remove mid drive disc exp material.");
        }

        if (highCount > 0)
        {
            var remove = inventoryService.RemoveItem(saveData.BackpackData, StrengthenMaterialIds.DriveDiscExpHigh, highCount);
            if (!remove.Success)
                return StrengthenApplyResult.Fail("Failed to remove high drive disc exp material.");
        }

        // 随机词条
        ApplyMinorRolls(driveDiscInstance, definition, oldLevel, preview.PreviewLevel);

        driveDiscInstance.Level = preview.PreviewLevel;
        driveDiscInstance.EnhanceLevel = preview.PreviewLevel;
        driveDiscInstance.CurrentLevelExp = preview.PreviewLevelExp;

        RebuildDriveDiscGrowthAttributes(driveDiscInstance, definition);

        return new StrengthenApplyResult
        {
            Success = true,
            OldLevel = oldLevel,
            OldLevelExp = oldExp,
            NewLevel = driveDiscInstance.Level,
            NewLevelExp = driveDiscInstance.CurrentLevelExp,
            ConsumedLowCount = lowCount,
            ConsumedMidCount = midCount,
            ConsumedHighCount = highCount
        };
    }

    private void ApplyMinorRolls(ItemInstanceData driveDiscInstance, DriveDiscDefinition definition, int oldLevel, int newLevel)
    {
        int[] triggerLevels = { 4, 8, 12, 15 };

        for (int i = 0; i < triggerLevels.Length; i++)
        {
            int trigger = triggerLevels[i];
            // 判断哪个区间升级的
            if (oldLevel < trigger && newLevel >= trigger)
            {
                // 随机命中副属性词条进行增加
                string rolledKey = RollMinorAttributeKey(definition);
                if (!string.IsNullOrEmpty(rolledKey))
                    driveDiscInstance.RolledMinorAttributeKeys.Add(rolledKey);
            }
        }
    }

    private string RollMinorAttributeKey(DriveDiscDefinition definition)
    {
        if (definition == null || definition.MinorAttributionKey == null || definition.MinorAttributionKey.Count <= 0)
            return string.Empty;

        // 随机一个词条索引
        int index = random.Next(0, definition.MinorAttributionKey.Count);
        return definition.MinorAttributionKey[index];
    }

    public void RebuildDriveDiscGrowthAttributes(ItemInstanceData driveDiscInstance, DriveDiscDefinition definition)
    {
        if (driveDiscInstance == null || definition == null)
            return;

        if (!string.IsNullOrEmpty(definition.MainAttributionKey))
        {
            // 主属性增加
            float mainGrowth = driveDiscInstance.Level * StrengthenBalanceConfig.GetDriveDiscMainGrowthPerLevel(definition.MainAttributionKey);
            ItemInstanceAttributeUtility.SetValue(driveDiscInstance.ExtraAttributes, definition.MainAttributionKey, mainGrowth);
        }

        // 副属性增加
        if (definition.MinorAttributionKey != null)
        {
            for (int i = 0; i < definition.MinorAttributionKey.Count; i++)
            {
                string key = definition.MinorAttributionKey[i];
                // 加强次数
                int rollCount = CountRolledKey(driveDiscInstance.RolledMinorAttributeKeys, key);
                float value = rollCount * StrengthenBalanceConfig.GetDriveDiscMinorRollGrowth(key);

                ItemInstanceAttributeUtility.SetValue(driveDiscInstance.ExtraAttributes, key, ItemInstanceAttributeUtility.GetValue(driveDiscInstance.ExtraAttributes, key) + 0f);
                ItemInstanceAttributeUtility.SetValue(driveDiscInstance.ExtraAttributes, key, value + GetBaseMinorValue(definition, key));
            }
        }
    }

    // 获得基础副属性值
    private float GetBaseMinorValue(DriveDiscDefinition definition, string key)
    {
        if (definition == null || definition.Attributes == null)
            return 0f;

        for (int i = 0; i < definition.Attributes.Count; i++)
        {
            if (definition.Attributes[i].Key == key)
                return definition.Attributes[i].Value;
        }

        return 0f;
    }

    private int CountRolledKey(List<string> keys, string targetKey)
    {
        if (keys == null || string.IsNullOrEmpty(targetKey))
            return 0;

        int count = 0;
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] == targetKey)
                count++;
        }

        return count;
    }

    private static void SimulateLevelUp(ref int level, ref int exp, int addedExp, int maxLevel, Func<int, int> getNeedExp)
    {
        if (addedExp <= 0)
            return;

        while (addedExp > 0 && level < maxLevel)
        {
            int need = getNeedExp(level);
            int remainNeed = need - exp;

            if (addedExp >= remainNeed)
            {
                addedExp -= remainNeed;
                level++;
                exp = 0;

                if (level >= maxLevel)
                {
                    exp = 0;
                    break;
                }
            }
            else
            {
                exp += addedExp;
                addedExp = 0;
            }
        }
    }

    private static void AddExpOnce(ref int level, ref int exp, int addExp, int maxLevel, Func<int, int> getNeedExp)
    {
        SimulateLevelUp(ref level, ref exp, addExp, maxLevel, getNeedExp);
    }
}