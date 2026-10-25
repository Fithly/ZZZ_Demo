using System;

public enum ItemType
{
    None = 0,

    Weapon = 1,          // 音擎
    DriveDisc = 2,       // 驱动盘
    Material = 3,        // 材料
    Consumable = 4,      // 消耗品
    Quest = 5,           // 任务物品
    Currency = 6         // 货币
}

// 区分当前装备槽位是音擎还是驱动盘
public enum EquipmentSlotCategoryType
{
    None = 0,
    Weapon = 1,
    DriveDisc = 2
}

public enum ItemSubType
{
    None = 0,

    // ===== Weapon =====
    WEngine = 1001,              // 音擎

    // ===== DriveDisc =====
    DriveDisc = 2001,            // 驱动盘

    // ===== Material =====
    LevelUpMaterial = 3001,      // 升级材料
    BreakthroughMaterial = 3002, // 突破材料
    CraftMaterial = 3003,        // 制作材料

    // ===== Consumable =====
    HpPotion = 4001,
    BuffPotion = 4002,
    ReviveItem = 4003,

    // ===== Quest =====
    QuestItem = 5001,

    // ===== Currency =====
    Dennies = 6001,              // 丁尼
    Polychrome = 6002            // 菲林
}

// 物品品质
public enum ItemQuality
{
    None = 0,
    Last = 1,
    Low = 2,
    Middle = 3,
    High = 4
}

// 驱动盘槽位（1~6号位）
public enum DriveDiscSlotType
{
    None = 0,
    One = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
}

// 驱动盘套装种类
public enum DriveDiscSetType
{
    None = 0,
    SetA = 1,
    SetB = 2,
    SetC = 3,
    SetD = 4,
    SetE = 5
}

// 材料用途分类
public enum MaterialPurposeType
{
    None = 0,

    // 升级材料
    CharacterLevelUp = 1,    // 角色等级升级
    WeaponLevelUp = 2,       // 武器等级升级
    DriveDiscLevelUp = 3,    // 驱动盘等级升级
}

// 背包容器类型
public enum InventoryContainerType
{
    None = 0,
    Backpack = 1,
    Equipment = 2
}

// 物品获取来源类型
public enum ItemAcquireSourceType
{
    None = 0,           // 
    Drop = 1,           // 掉落
    Reward = 2,         // 奖励
    Shop = 3,           // 商店
    Craft = 4,          // 制作
}