using System;
using UnityEngine;
using XLua;

/// <summary>
/// Lua Demo 桥接层
/// 
/// 职责：
/// 1. 从 Lua 读取奖励配置
/// 2. 让 Lua 调用 C# 发物
/// 3. 使用当前账号专属存档保存数据
/// 4. 使用 ResultPanel 显示 Lua 配置的文本
/// 
/// 注意：
/// Lua 不直接写 GameSaveData，避免数据结构失控。
/// </summary>
[LuaCallCSharp]
public class LuaDemoBridge
{
    public static LuaDemoBridge Instance { get; private set; }

    private const string ConfigModuleName = "Demo/LuaDemoConfig";
    private const string RewardClaimPrefix = "lua_reward_";

    private GameSaveData saveData;
    private SaveGameService saveGameService;
    private InventoryService inventoryService;

    private LuaTable configTable;
    private LuaTable textsTable;
    private LuaTable rewardsTable;

    public void Initialize(
        GameSaveData saveData,
        SaveGameService saveGameService,
        InventoryService inventoryService)
    {
        this.saveData = saveData;
        this.saveGameService = saveGameService;
        this.inventoryService = inventoryService;

        Instance = this;

        LoadConfig();
    }

    private void LoadConfig()
    {
        LuaManager.Instance.Initialize();

        // require 后把返回值挂到全局，方便 C# 取表
        LuaManager.Instance.DoString($"LuaDemoConfig = require('{ConfigModuleName}')");

        LuaTable global = LuaManager.Instance.Global;
        if (global == null)
        {
            Debug.LogError("[LuaDemoBridge] Lua Global is null.");
            return;
        }

        configTable = global.Get<LuaTable>("LuaDemoConfig");
        if (configTable == null)
        {
            Debug.LogError("[LuaDemoBridge] LuaDemoConfig 加载失败.");
            return;
        }

        textsTable = configTable.Get<LuaTable>("Texts");
        rewardsTable = configTable.Get<LuaTable>("Rewards");
    }

    public void CallLuaFunction(string functionName)
    {
        if (configTable == null)
            LoadConfig();

        if (configTable == null || string.IsNullOrEmpty(functionName))
            return;

        LuaDemoFunction luaFunction = configTable.Get<LuaDemoFunction>(functionName);
        if (luaFunction == null)
        {
            Debug.LogWarning($"[LuaDemoBridge] Lua 函数不存在: {functionName}");
            return;
        }

        luaFunction(this);
    }

    // 在Lua中被调用（可以是领取登录奖励 也可以是测试发物）
    public void GrantRewardById(string rewardId)
    {
        if (string.IsNullOrEmpty(rewardId))
            return;

        if (saveData == null || saveGameService == null || inventoryService == null)
        {
            Debug.LogError("[LuaDemoBridge] Bridge 尚未初始化.");
            return;
        }

        if (rewardsTable == null)
        {
            Debug.Log("物品配置不存在");
            //ShowText("RewardConfigMissing");
            return;
        }

        LuaTable rewardTable = rewardsTable.Get<LuaTable>(rewardId);
        if (rewardTable == null)
        {
            Debug.Log("物品配置不存在");

            //ShowText("RewardConfigMissing");
            return;
        }

        // 安全读取 Once。
        // 如果 Lua 中没有配置 Once，则默认 false。
        bool once = GetBoolOrDefault(rewardTable, "Once", false);

        // 安全读取 SkipExistingItems。
        // 登录奖励一般 false，测试发物一般 true。
        bool skipExistingItems = GetBoolOrDefault(rewardTable, "SkipExistingItems", false);

        // 如果旧版本曾经把领取记录写进 FinishedQuestIds，
        // 这里会迁移成新的 LuaRewardClaimRecords。
        bool migratedLegacyRecord = TryMigrateLegacyRewardClaimRecord(rewardId, once);

        // 如果新 Json 里已经有这个奖励记录，并且当前 Lua 配置的 Once 有变化，
        // 同步更新记录里的 Once 字段，保证 Json 里能看到最新配置含义。
        bool updatedOnceFlag = UpdateRewardOnceFlagIfExists(rewardId, once);

        // Once 只检查“奖励ID是否领过”，不检查背包是否已有该物品。
        if (once && IsRewardClaimed(rewardId))
        {
            if (migratedLegacyRecord || updatedOnceFlag)
                saveGameService.SaveForCurrentAccount(saveData);

            Debug.Log("奖励已经领过了");
            ShowText("RewardAlreadyClaimed");
            return;
        }

        LuaTable itemList = rewardTable.Get<LuaTable>("Items");
        if (itemList == null)
        {
            Debug.Log("物品配置错误");
            //ShowText("RewardItemInvalid");
            return;
        }

        bool hasAnyItemConfig = false;
        bool hasAnyItemAdded = false;
        bool hasAnyItemSkipped = false;

        for (int i = 1; ; i++)
        {
            LuaTable itemTable = itemList.Get<int, LuaTable>(i);
            if (itemTable == null)
                break;

            int itemId = itemTable.Get<int>("ItemId");
            int count = itemTable.Get<int>("Count");

            if (itemId <= 0 || count <= 0)
                continue;

            hasAnyItemConfig = true;

            // 只有测试发物这类配置开启 SkipExistingItems 时，
            // 才跳过背包里已有的物品。
            if (skipExistingItems)
            {
                int ownedCount = inventoryService.GetItemCount(saveData.BackpackData, itemId).TotalCount;
                if (ownedCount > 0)
                {
                    hasAnyItemSkipped = true;
                    Debug.Log($"[LuaDemoBridge] 跳过发放. RewardId={rewardId}, ItemId={itemId}, Owned={ownedCount}");
                    continue;
                }
            }

            bool success = inventoryService.AddItemWithAutoExpand(
                saveData.BackpackData,
                itemId,
                count);

            if (success)
                hasAnyItemAdded = true;
        }

        if (!hasAnyItemConfig)
        {
            Debug.Log("物品配置出错");

            //ShowText("RewardItemInvalid");
            return;
        }

        if (!hasAnyItemAdded)
        {
            if (hasAnyItemSkipped)
                Debug.Log("物品存在，跳过发物");
                //ShowText("RewardAllItemsAlreadyOwned");
            else
                Debug.Log("物品配置出错");
                //ShowText("RewardItemInvalid");

            return;
        }

        // 领取成功后，统一登记到 Json。
        // Once 也会一起写进 LuaRewardClaimRecords。
        RegisterRewardClaimRecord(rewardId, once);

        saveGameService.SaveForCurrentAccount(saveData);

        string successTextKey = rewardTable.Get<string>("SuccessTextKey");
        Debug.Log("successTextKey: " + successTextKey);
        ShowText(string.IsNullOrEmpty(successTextKey) ? "TestGrantSuccess" : successTextKey);
    }

    public string GetText(string key)
    {
        if (textsTable == null || string.IsNullOrEmpty(key))
            return key;

        string value = textsTable.Get<string>(key);
        return string.IsNullOrEmpty(value) ? key : value;
    }

    public void ShowText(string key)
    {
        string text = GetText(key);

        if (ResultPanel.Instance != null)
            ResultPanel.Instance.ShowResult(text);
        else
            Debug.Log(text);
    }

    // 安全读取 Lua bool。
    // 如果 Lua 中没有该字段，返回默认值。
    private bool GetBoolOrDefault(LuaTable table, string key, bool defaultValue)
    {
        if (table == null || string.IsNullOrEmpty(key))
            return defaultValue;

        object value = table.Get<object>(key);
        if (value == null)
            return defaultValue;

        if (value is bool boolValue)
            return boolValue;

        if (value is string stringValue)
        {
            if (bool.TryParse(stringValue, out bool parsedValue))
                return parsedValue;
        }

        return defaultValue;
    }

    // 判断奖励是否已经领取过
    private bool IsRewardClaimed(string rewardId)
    {
        LuaRewardClaimData record = GetRewardClaimRecord(rewardId);
        if (record != null && record.ClaimCount > 0)
            return true;

        // 兼容旧版本 FinishedQuestIds 记录
        return IsLegacyRewardClaimed(rewardId);
    }

    // 登记领取记录
    private void RegisterRewardClaimRecord(string rewardId, bool once)
    {
        if (string.IsNullOrEmpty(rewardId))
            return;

        if (saveData.LuaRewardClaimRecords == null)
            saveData.LuaRewardClaimRecords = new System.Collections.Generic.List<LuaRewardClaimData>();

        LuaRewardClaimData record = GetRewardClaimRecord(rewardId);
        if (record == null)
        {
            record = new LuaRewardClaimData
            {
                RewardId = rewardId,
                Once = once,
                ClaimCount = 0,
                LastClaimTimeTicks = 0
            };

            saveData.LuaRewardClaimRecords.Add(record);
        }

        // 每次领取时，把当前 Lua 配置中的 Once 状态也登记进去。
        record.Once = once;
        record.ClaimCount++;
        record.LastClaimTimeTicks = DateTime.UtcNow.Ticks;
    }

    // 查找奖励领取记录
    private LuaRewardClaimData GetRewardClaimRecord(string rewardId)
    {
        if (saveData == null || saveData.LuaRewardClaimRecords == null || string.IsNullOrEmpty(rewardId))
            return null;

        for (int i = 0; i < saveData.LuaRewardClaimRecords.Count; i++)
        {
            LuaRewardClaimData record = saveData.LuaRewardClaimRecords[i];
            if (record == null)
                continue;

            if (record.RewardId == rewardId)
                return record;
        }

        return null;
    }

    // 如果记录已经存在，则同步 Once 字段
    private bool UpdateRewardOnceFlagIfExists(string rewardId, bool once)
    {
        LuaRewardClaimData record = GetRewardClaimRecord(rewardId);
        if (record == null)
            return false;

        if (record.Once == once)
            return false;

        record.Once = once;
        return true;
    }

    // 兼容旧版本：判断 FinishedQuestIds 中是否有旧奖励记录
    private bool IsLegacyRewardClaimed(string rewardId)
    {
        string finalId = RewardClaimPrefix + rewardId;

        if (saveData == null || saveData.PlayerProgressData == null)
            return false;

        if (saveData.PlayerProgressData.FinishedQuestIds == null)
            return false;

        return saveData.PlayerProgressData.FinishedQuestIds.Contains(finalId);
    }

    // 兼容旧版本：把 FinishedQuestIds 中的旧奖励记录迁移到 LuaRewardClaimRecords
    private bool TryMigrateLegacyRewardClaimRecord(string rewardId, bool once)
    {
        if (!IsLegacyRewardClaimed(rewardId))
            return false;

        LuaRewardClaimData record = GetRewardClaimRecord(rewardId);
        if (record != null)
        {
            if (record.Once != once)
            {
                record.Once = once;
                return true;
            }

            return false;
        }

        if (saveData.LuaRewardClaimRecords == null)
            saveData.LuaRewardClaimRecords = new System.Collections.Generic.List<LuaRewardClaimData>();

        saveData.LuaRewardClaimRecords.Add(new LuaRewardClaimData
        {
            RewardId = rewardId,
            Once = once,
            ClaimCount = 1,
            LastClaimTimeTicks = DateTime.UtcNow.Ticks
        });

        return true;
    }
}