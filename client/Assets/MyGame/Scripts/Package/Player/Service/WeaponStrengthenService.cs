using System;

/// <summary>
/// 武器等级升级服务
/// </summary>
public class WeaponStrengthenService
{
    private readonly InventoryService inventoryService;

    public WeaponStrengthenService(InventoryService inventoryService)
    {
        this.inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
    }

    // 预览
    public StrengthenPreviewResult Preview(GameSaveData saveData, ItemInstanceData weaponInstance, WeaponDefinition definition, int lowCount, int midCount, int highCount)
    {
        if (saveData == null || saveData.BackpackData == null)
            return StrengthenPreviewResult.Fail("SaveData invalid.");

        if (weaponInstance == null || definition == null)
            return StrengthenPreviewResult.Fail("Weapon target invalid.");

        // 拥有数量
        int ownedLow = inventoryService.GetItemCount(saveData.BackpackData, StrengthenMaterialIds.WeaponExpLow).TotalCount;
        int ownedMid = inventoryService.GetItemCount(saveData.BackpackData, StrengthenMaterialIds.WeaponExpMid).TotalCount;
        int ownedHigh = inventoryService.GetItemCount(saveData.BackpackData, StrengthenMaterialIds.WeaponExpHigh).TotalCount;

        if (lowCount < 0 || midCount < 0 || highCount < 0)
            return StrengthenPreviewResult.Fail("Selected material count cannot be negative.");

        if (lowCount > ownedLow || midCount > ownedMid || highCount > ownedHigh)
            return StrengthenPreviewResult.Fail("Selected material count exceeds owned amount.");

        // 增添的经验值
        int addedExp =
            lowCount * StrengthenBalanceConfig.WeaponExpLowValue +
            midCount * StrengthenBalanceConfig.WeaponExpMidValue +
            highCount * StrengthenBalanceConfig.WeaponExpHighValue;

        int level = weaponInstance.Level;
        int exp = weaponInstance.CurrentLevelExp;

        // 模拟升级
        SimulateLevelUp(
            ref level,
            ref exp,
            addedExp,
            StrengthenBalanceConfig.WeaponMaxLevel,
            StrengthenBalanceConfig.GetWeaponNeedExp);

        var result = StrengthenPreviewResult.Ok();
        result.CurrentLevel = weaponInstance.Level;
        result.CurrentLevelExp = weaponInstance.CurrentLevelExp;
        result.PreviewLevel = level;
        result.PreviewLevelExp = exp;
        result.AddedExp = addedExp;
        result.GainedLevelCount = Math.Max(0, level - weaponInstance.Level);
        result.ReachedMaxLevel = level >= StrengthenBalanceConfig.WeaponMaxLevel;

        result.MaterialSelections.Add(new StrengthenMaterialSelection
        {
            ItemId = StrengthenMaterialIds.WeaponExpLow,
            OwnedCount = ownedLow,
            SelectedCount = lowCount,
            ExpPerItem = StrengthenBalanceConfig.WeaponExpLowValue
        });

        result.MaterialSelections.Add(new StrengthenMaterialSelection
        {
            ItemId = StrengthenMaterialIds.WeaponExpMid,
            OwnedCount = ownedMid,
            SelectedCount = midCount,
            ExpPerItem = StrengthenBalanceConfig.WeaponExpMidValue
        });

        result.MaterialSelections.Add(new StrengthenMaterialSelection
        {
            ItemId = StrengthenMaterialIds.WeaponExpHigh,
            OwnedCount = ownedHigh,
            SelectedCount = highCount,
            ExpPerItem = StrengthenBalanceConfig.WeaponExpHighValue
        });

        return result;
    }

    // 模拟自动添加
    public StrengthenPreviewResult BuildAutoPlan(GameSaveData saveData, ItemInstanceData weaponInstance, WeaponDefinition definition)
    {
        if (saveData == null || weaponInstance == null || definition == null)
            return StrengthenPreviewResult.Fail("Weapon target invalid.");

        int lowOwned = inventoryService.GetItemCount(saveData.BackpackData, StrengthenMaterialIds.WeaponExpLow).TotalCount;
        int midOwned = inventoryService.GetItemCount(saveData.BackpackData, StrengthenMaterialIds.WeaponExpMid).TotalCount;
        int highOwned = inventoryService.GetItemCount(saveData.BackpackData, StrengthenMaterialIds.WeaponExpHigh).TotalCount;

        int low = 0;
        int mid = 0;
        int high = 0;

        int level = weaponInstance.Level;
        int exp = weaponInstance.CurrentLevelExp;

        while (level < StrengthenBalanceConfig.WeaponMaxLevel)
        {
            int need = StrengthenBalanceConfig.GetWeaponNeedExp(level) - exp;
            if (need <= 0)
            {
                level++;
                exp = 0;
                continue;
            }

            // 优先尝试使用高级经验材料（经验值最大），但为避免大量溢出浪费，只在“高级材料经验值 <= 所需经验 × 3”时使用
            // 条件是：高级材料还有剩余，且满足不严重溢出的阈值
            if (high < highOwned && StrengthenBalanceConfig.WeaponExpHighValue <= need * 3)
            {
                high++;
                AddExpOnce(ref level, ref exp, StrengthenBalanceConfig.WeaponExpHighValue,
                    StrengthenBalanceConfig.WeaponMaxLevel, StrengthenBalanceConfig.GetWeaponNeedExp);
                continue;
            }

            // 其次尝试使用中级经验材料，同样有经验溢出阈值（中级材料经验值 <= 所需经验 × 2）
            if (mid < midOwned && StrengthenBalanceConfig.WeaponExpMidValue <= need * 2)
            {
                mid++;
                AddExpOnce(ref level, ref exp, StrengthenBalanceConfig.WeaponExpMidValue,
                    StrengthenBalanceConfig.WeaponMaxLevel, StrengthenBalanceConfig.GetWeaponNeedExp);
                continue;
            }

            // 再尝试使用低级经验材料，这里没有溢出阈值限制，因为低级材料经验很少，可以接受溢出
            if (low < lowOwned)
            {
                low++;
                AddExpOnce(ref level, ref exp, StrengthenBalanceConfig.WeaponExpLowValue,
                    StrengthenBalanceConfig.WeaponMaxLevel, StrengthenBalanceConfig.GetWeaponNeedExp);
                continue;
            }

            // 如果低级材料也用完了，但仍未满级，尝试放宽中级材料的使用限制（无视溢出阈值）
            if (mid < midOwned)
            {
                mid++;
                AddExpOnce(ref level, ref exp, StrengthenBalanceConfig.WeaponExpMidValue,
                    StrengthenBalanceConfig.WeaponMaxLevel, StrengthenBalanceConfig.GetWeaponNeedExp);
                continue;
            }

            // 同样放宽高级材料的使用限制
            if (high < highOwned)
            {
                high++;
                AddExpOnce(ref level, ref exp, StrengthenBalanceConfig.WeaponExpHighValue,
                    StrengthenBalanceConfig.WeaponMaxLevel, StrengthenBalanceConfig.GetWeaponNeedExp);
                continue;
            }

            // 所有材料都已经没有剩余，无法继续升级
            break;
        }

        return Preview(saveData, weaponInstance, definition, low, mid, high);
    }

    public StrengthenApplyResult Apply(GameSaveData saveData, ItemInstanceData weaponInstance, WeaponDefinition definition, int lowCount, int midCount, int highCount)
    {
        var preview = Preview(saveData, weaponInstance, definition, lowCount, midCount, highCount);
        if (!preview.Success)
            return StrengthenApplyResult.Fail(preview.ErrorMessage);

        if (preview.AddedExp <= 0)
            return StrengthenApplyResult.Fail("No exp added.");

        int oldLevel = weaponInstance.Level;
        int oldExp = weaponInstance.CurrentLevelExp;

        if (lowCount > 0)
        {
            var remove = inventoryService.RemoveItem(saveData.BackpackData, StrengthenMaterialIds.WeaponExpLow, lowCount);
            if (!remove.Success)
                return StrengthenApplyResult.Fail("Failed to remove low weapon exp material.");
        }

        if (midCount > 0)
        {
            var remove = inventoryService.RemoveItem(saveData.BackpackData, StrengthenMaterialIds.WeaponExpMid, midCount);
            if (!remove.Success)
                return StrengthenApplyResult.Fail("Failed to remove mid weapon exp material.");
        }

        if (highCount > 0)
        {
            var remove = inventoryService.RemoveItem(saveData.BackpackData, StrengthenMaterialIds.WeaponExpHigh, highCount);
            if (!remove.Success)
                return StrengthenApplyResult.Fail("Failed to remove high weapon exp material.");
        }

        weaponInstance.Level = preview.PreviewLevel;
        weaponInstance.EnhanceLevel = preview.PreviewLevel;
        weaponInstance.CurrentLevelExp = preview.PreviewLevelExp;

        // 武器属性增加
        RebuildWeaponGrowthAttributes(weaponInstance, definition);

        return new StrengthenApplyResult
        {
            Success = true,
            OldLevel = oldLevel,
            OldLevelExp = oldExp,
            NewLevel = weaponInstance.Level,
            NewLevelExp = weaponInstance.CurrentLevelExp,
            ConsumedLowCount = lowCount,
            ConsumedMidCount = midCount,
            ConsumedHighCount = highCount
        };
    }

    public void RebuildWeaponGrowthAttributes(ItemInstanceData weaponInstance, WeaponDefinition definition)
    {
        if (weaponInstance == null || definition == null)
            return;

        // 键
        string baseKey = definition.BaseAttributionKey;
        string advanceKey = definition.AdvanceAttributionKey;

        if (!string.IsNullOrEmpty(baseKey))
        {
            // 获取增长值
            float baseGrowth = weaponInstance.Level * StrengthenBalanceConfig.GetWeaponBaseGrowthPerLevel(baseKey);
            // 设置增长后的属性值
            ItemInstanceAttributeUtility.SetValue(weaponInstance.ExtraAttributes, baseKey, baseGrowth);
        }

        if (!string.IsNullOrEmpty(advanceKey))
        {
            // 每十级增长一次高级属性
            int tenLevelCount = weaponInstance.Level / 10;
            float advanceGrowth = tenLevelCount * StrengthenBalanceConfig.GetWeaponAdvanceGrowthPerTenLevel(advanceKey);
            ItemInstanceAttributeUtility.SetValue(weaponInstance.ExtraAttributes, advanceKey, advanceGrowth);
        }
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