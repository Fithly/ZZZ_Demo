using System;

/// <summary>
/// 定义升级逻辑
/// </summary>
public static class StrengthenBalanceConfig
{
    // 最大等级
    public const int CharacterMaxLevel = 60;
    public const int WeaponMaxLevel = 60;
    public const int DriveDiscMaxLevel = 15;

    // 角色等级材料经验值
    public const int CharacterExpLowValue = 100;
    public const int CharacterExpMidValue = 500;
    public const int CharacterExpHighValue = 2000;

    // 武器等级材料经验值
    public const int WeaponExpLowValue = 100;
    public const int WeaponExpMidValue = 500;
    public const int WeaponExpHighValue = 2000;

    // 驱动盘等级材料经验值
    public const int DriveDiscExpLowValue = 80;
    public const int DriveDiscExpMidValue = 400;
    public const int DriveDiscExpHighValue = 1600;

    // 角色升级增加值
    public const int CharacterHpPerLevel = 18;
    public const float CharacterAttackPerLevel = 2.4f;
    public const float CharacterDefensePerLevel = 1.6f;

    // 获得角色升级所需经验值
    public static int GetCharacterNeedExp(int level)
    {
        if (level >= CharacterMaxLevel)
            return 0;

        // 升级经验公式
        return 100 + (level - 1) * 20;
    }

    // 获得武器升级所需经验值
    public static int GetWeaponNeedExp(int level)
    {
        if (level >= WeaponMaxLevel)
            return 0;

        return 80 + (level - 1) * 18;
    }

    // 获得驱动盘升级所需经验值
    public static int GetDriveDiscNeedExp(int level)
    {
        if (level >= DriveDiscMaxLevel)
            return 0;

        return 60 + level * 15;
    }

    // 获取武器基础属性每级成长值
    public static float GetWeaponBaseGrowthPerLevel(string attributeKey)
    {
        switch (attributeKey)
        {
            case AttributeKeyUtility.HP: return 25f;
            case AttributeKeyUtility.Attack: return 5f;
            case AttributeKeyUtility.Defense: return 4f;
            case AttributeKeyUtility.CritRate: return 0.002f;
            case AttributeKeyUtility.CritDamage: return 0.010f;
            default: return 0f;
        }
    }

    // 获取武器高级属性每级成长值
    public static float GetWeaponAdvanceGrowthPerTenLevel(string attributeKey)
    {
        switch (attributeKey)
        {
            case AttributeKeyUtility.HP: return 120f;
            case AttributeKeyUtility.Attack: return 20f;
            case AttributeKeyUtility.Defense: return 15f;
            case AttributeKeyUtility.CritRate: return 0.030f;
            case AttributeKeyUtility.CritDamage: return 0.060f;
            default: return 0f;
        }
    }

    // 获取驱动盘主属性每级成长值
    public static float GetDriveDiscMainGrowthPerLevel(string attributeKey)
    {
        switch (attributeKey)
        {
            case AttributeKeyUtility.HP: return 12f;
            case AttributeKeyUtility.Attack: return 2.5f;
            case AttributeKeyUtility.Defense: return 2f;
            case AttributeKeyUtility.CritRate: return 0.005f;
            case AttributeKeyUtility.CritDamage: return 0.010f;
            default: return 0f;
        }
    }

    // 获取驱动盘副属性每级成长值
    public static float GetDriveDiscMinorRollGrowth(string attributeKey)
    {
        switch (attributeKey)
        {
            case AttributeKeyUtility.HP: return 20f;
            case AttributeKeyUtility.Attack: return 3f;
            case AttributeKeyUtility.Defense: return 3f;
            case AttributeKeyUtility.CritRate: return 0.010f;
            case AttributeKeyUtility.CritDamage: return 0.020f;
            default: return 0f;
        }
    }
}