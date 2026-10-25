using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public int SaveVersion = 1; // 保存版本
    public long SaveTimeTicks;  // 保存时间

    public PlayerBaseData PlayerBaseData = new PlayerBaseData();
    public PlayerAttributeData PlayerAttributeData = new PlayerAttributeData();
    public PlayerProgressData PlayerProgressData = new PlayerProgressData();

    // 用于记录登录奖励、活动奖励、测试发物等 Lua 配置奖励的领取状态。
    public List<LuaRewardClaimData> LuaRewardClaimRecords = new List<LuaRewardClaimData>(); // Lua奖励领取记录

    public InventoryData BackpackData = new InventoryData();

    public ItemInstanceData EquippedWeapon = null;            // 当前装备音擎
    public DriveDiscEquipData DriveDiscEquipData = new DriveDiscEquipData(); // 当前装备驱动盘

    public static GameSaveData CreateDefault()
    {
        var save = new GameSaveData();

        save.SaveVersion = 1;
        save.SaveTimeTicks = DateTime.UtcNow.Ticks;

        save.PlayerBaseData.PlayerId = Guid.NewGuid().ToString("N");
        save.PlayerBaseData.PlayerName = "Player";
        save.PlayerBaseData.Level = 1;
        save.PlayerBaseData.Exp = 0;
        save.PlayerBaseData.CurrentLevelExp = 0;
        save.PlayerBaseData.Gold = 0;
        save.PlayerBaseData.Diamond = 0;

        save.PlayerAttributeData.BaseHP = 100;
        save.PlayerAttributeData.BaseAttack = 10;
        save.PlayerAttributeData.BaseDefense = 5;
        save.PlayerAttributeData.BaseCritRate = 0.05f;
        save.PlayerAttributeData.BaseCritDamage = 0.50f;

        save.PlayerProgressData.CurrentStageId = "stage_001";
        // 默认存档初始化 Lua 奖励领取记录。
        save.LuaRewardClaimRecords = new List<LuaRewardClaimData>();

        save.BackpackData.ContainerType = InventoryContainerType.Backpack;
        save.BackpackData.Capacity = InventoryGridPresenter.DefaultCapacity;
        save.BackpackData.Slots.Clear();

        // 给背包添加槽位数据
        for (int i = 0; i < save.BackpackData.Capacity; i++)
        {
            save.BackpackData.Slots.Add(new InventorySlotData
            {
                SlotIndex = i,
                IsUnlocked = true,
                Item = null
            });
        }

        // 明确初始化装备区为空，避免任何旧引用或脏值残留
        save.EquippedWeapon = null;
        save.DriveDiscEquipData = new DriveDiscEquipData
        {
            One = null,
            Two = null,
            Three = null,
            Four = null,
            Five = null,
            Six = null
        };

        return save;
    }

    public static GameSaveData CreateDefaultForAccount(string accountId, string accountName)
    {
        var save = CreateDefault();

        if (!string.IsNullOrEmpty(accountId))
            save.PlayerBaseData.PlayerId = accountId;

        if (!string.IsNullOrEmpty(accountName))
            save.PlayerBaseData.PlayerName = accountName;

        return save;
    }
}