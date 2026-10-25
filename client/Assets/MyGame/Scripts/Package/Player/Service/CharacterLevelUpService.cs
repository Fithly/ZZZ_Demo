using System;

/// <summary>
/// 角色等级升级服务
/// </summary>
public class CharacterLevelUpService
{
    private readonly InventoryService inventoryService;

    public CharacterLevelUpService(InventoryService inventoryService)
    {
        this.inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
    }

    // 预览
    public StrengthenPreviewResult Preview(GameSaveData saveData, PlayerBaseData playerData, PlayerAttributeData playerattrData, int lowCount, int midCount, int highCount)
    {
        if (saveData == null || saveData.BackpackData == null)
            return StrengthenPreviewResult.Fail("SaveData invalid.");

        if (playerData == null || playerattrData == null)
            return StrengthenPreviewResult.Fail("player target invalid.");

        // 拥有数量
        int ownedLow = inventoryService.GetItemCount(saveData.BackpackData, StrengthenMaterialIds.CharacterExpLow).TotalCount;
        int ownedMid = inventoryService.GetItemCount(saveData.BackpackData, StrengthenMaterialIds.CharacterExpMid).TotalCount;
        int ownedHigh = inventoryService.GetItemCount(saveData.BackpackData, StrengthenMaterialIds.CharacterExpHigh).TotalCount;

        if (lowCount < 0 || midCount < 0 || highCount < 0)
            return StrengthenPreviewResult.Fail("Selected material count cannot be negative.");

        if (lowCount > ownedLow || midCount > ownedMid || highCount > ownedHigh)
            return StrengthenPreviewResult.Fail("Selected material count exceeds owned amount.");

        // 增添的经验值
        int addedExp =
            lowCount * StrengthenBalanceConfig.CharacterExpLowValue +
            midCount * StrengthenBalanceConfig.CharacterExpMidValue +
            highCount * StrengthenBalanceConfig.CharacterExpHighValue;

        int level = playerData.Level;
        int exp = playerData.CurrentLevelExp;

        // 模拟升级
        SimulateLevelUp(
            ref level,
            ref exp,
            addedExp,
            StrengthenBalanceConfig.CharacterMaxLevel,
            StrengthenBalanceConfig.GetCharacterNeedExp);

        var result = StrengthenPreviewResult.Ok();
        result.CurrentLevel = playerData.Level;
        result.CurrentLevelExp = playerData.CurrentLevelExp;
        result.PreviewLevel = level;
        result.PreviewLevelExp = exp;
        result.AddedExp = addedExp;
        result.GainedLevelCount = Math.Max(0, level - playerData.Level);
        result.ReachedMaxLevel = level >= StrengthenBalanceConfig.CharacterMaxLevel;

        result.MaterialSelections.Add(new StrengthenMaterialSelection
        {
            ItemId = StrengthenMaterialIds.CharacterExpLow,
            OwnedCount = ownedLow,
            SelectedCount = lowCount,
            ExpPerItem = StrengthenBalanceConfig.CharacterExpLowValue
        });

        result.MaterialSelections.Add(new StrengthenMaterialSelection
        {
            ItemId = StrengthenMaterialIds.CharacterExpMid,
            OwnedCount = ownedMid,
            SelectedCount = midCount,
            ExpPerItem = StrengthenBalanceConfig.CharacterExpMidValue
        });

        result.MaterialSelections.Add(new StrengthenMaterialSelection
        {
            ItemId = StrengthenMaterialIds.CharacterExpHigh,
            OwnedCount = ownedHigh,
            SelectedCount = highCount,
            ExpPerItem = StrengthenBalanceConfig.CharacterExpHighValue
        });

        return result;
    }

    // 模拟自动添加
    public StrengthenPreviewResult BuildAutoPlan(GameSaveData saveData, PlayerBaseData playerData, PlayerAttributeData playerattrData)
    {
        if (saveData == null || playerData == null || playerattrData == null)
            return StrengthenPreviewResult.Fail("character target invalid.");

        int lowOwned = inventoryService.GetItemCount(saveData.BackpackData, StrengthenMaterialIds.CharacterExpLow).TotalCount;
        int midOwned = inventoryService.GetItemCount(saveData.BackpackData, StrengthenMaterialIds.CharacterExpMid).TotalCount;
        int highOwned = inventoryService.GetItemCount(saveData.BackpackData, StrengthenMaterialIds.CharacterExpHigh).TotalCount;

        int low = 0;
        int mid = 0;
        int high = 0;

        int level = playerData.Level;
        int exp = playerData.CurrentLevelExp;

        while (level < StrengthenBalanceConfig.CharacterMaxLevel)
        {
            int need = StrengthenBalanceConfig.GetCharacterNeedExp(level) - exp;
            if (need <= 0)
            {
                level++;
                exp = 0;
                continue;
            }

            // 优先尝试使用高级经验材料（经验值最大），但为避免大量溢出浪费，只在“高级材料经验值 <= 所需经验 × 3”时使用
            // 条件是：高级材料还有剩余，且满足不严重溢出的阈值
            if (high < highOwned && StrengthenBalanceConfig.CharacterExpHighValue <= need * 3)
            {
                high++;
                AddExpOnce(ref level, ref exp, StrengthenBalanceConfig.CharacterExpHighValue,
                    StrengthenBalanceConfig.CharacterMaxLevel, StrengthenBalanceConfig.GetCharacterNeedExp);
                continue;
            }

            // 其次尝试使用中级经验材料，同样有经验溢出阈值（中级材料经验值 <= 所需经验 × 2）
            if (mid < midOwned && StrengthenBalanceConfig.CharacterExpMidValue <= need * 2)
            {
                mid++;
                AddExpOnce(ref level, ref exp, StrengthenBalanceConfig.CharacterExpMidValue,
                    StrengthenBalanceConfig.CharacterMaxLevel, StrengthenBalanceConfig.GetCharacterNeedExp);
                continue;
            }

            // 再尝试使用低级经验材料，这里没有溢出阈值限制，因为低级材料经验很少，可以接受溢出
            if (low < lowOwned)
            {
                low++;
                AddExpOnce(ref level, ref exp, StrengthenBalanceConfig.CharacterExpLowValue,
                    StrengthenBalanceConfig.CharacterMaxLevel, StrengthenBalanceConfig.GetCharacterNeedExp);
                continue;
            }

            // 如果低级材料也用完了，但仍未满级，尝试放宽中级材料的使用限制（无视溢出阈值）
            if (mid < midOwned)
            {
                mid++;
                AddExpOnce(ref level, ref exp, StrengthenBalanceConfig.CharacterExpMidValue,
                    StrengthenBalanceConfig.CharacterMaxLevel, StrengthenBalanceConfig.GetCharacterNeedExp);
                continue;
            }

            // 同样放宽高级材料的使用限制
            if (high < highOwned)
            {
                high++;
                AddExpOnce(ref level, ref exp, StrengthenBalanceConfig.CharacterExpHighValue,
                    StrengthenBalanceConfig.CharacterMaxLevel, StrengthenBalanceConfig.GetCharacterNeedExp);
                continue;
            }

            // 所有材料都已经没有剩余，无法继续升级
            break;
        }

        return Preview(saveData, playerData, playerattrData, low, mid, high);
    }

    public StrengthenApplyResult Apply(GameSaveData saveData, PlayerBaseData playerData, PlayerAttributeData playerattrData, int lowCount, int midCount, int highCount)
    {
        var preview = Preview(saveData, playerData, playerattrData, lowCount, midCount, highCount);
        if (!preview.Success)
            return StrengthenApplyResult.Fail(preview.ErrorMessage);

        if (preview.AddedExp <= 0)
            return StrengthenApplyResult.Fail("No exp added.");

        int oldLevel = playerData.Level;
        int oldExp = playerData.CurrentLevelExp;

        if (lowCount > 0)
        {
            var remove = inventoryService.RemoveItem(saveData.BackpackData, StrengthenMaterialIds.CharacterExpLow, lowCount);
            if (!remove.Success)
                return StrengthenApplyResult.Fail("Failed to remove low weapon exp material.");
        }

        if (midCount > 0)
        {
            var remove = inventoryService.RemoveItem(saveData.BackpackData, StrengthenMaterialIds.CharacterExpMid, midCount);
            if (!remove.Success)
                return StrengthenApplyResult.Fail("Failed to remove mid weapon exp material.");
        }

        if (highCount > 0)
        {
            var remove = inventoryService.RemoveItem(saveData.BackpackData, StrengthenMaterialIds.CharacterExpHigh, highCount);
            if (!remove.Success)
                return StrengthenApplyResult.Fail("Failed to remove high weapon exp material.");
        }

        playerData.Level = preview.PreviewLevel;
        playerData.EnhanceLevel = preview.PreviewLevel;
        playerData.CurrentLevelExp = preview.PreviewLevelExp;

        // 武器属性增加
        RebuildCharacterGrowthAttributes(playerData, playerattrData);

        return new StrengthenApplyResult
        {
            Success = true,
            OldLevel = oldLevel,
            OldLevelExp = oldExp,
            NewLevel = playerData.Level,
            NewLevelExp = playerData.CurrentLevelExp,
            ConsumedLowCount = lowCount,
            ConsumedMidCount = midCount,
            ConsumedHighCount = highCount
        };
    }

    public void RebuildCharacterGrowthAttributes(PlayerBaseData playerData, PlayerAttributeData playerattrData)
    {
        if (playerData == null || playerattrData == null)
            return;

        // 获取增长值
        float hpGrowth = playerData.Level * StrengthenBalanceConfig.CharacterHpPerLevel;
        float attackGrowth = playerData.Level * StrengthenBalanceConfig.CharacterAttackPerLevel;
        float defenceGrowth = playerData.Level * StrengthenBalanceConfig.CharacterDefensePerLevel;

        // 设置增长后的属性值
        playerattrData.BaseHP += hpGrowth;
        playerattrData.BaseAttack += attackGrowth;
        playerattrData.BaseDefense += defenceGrowth;
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