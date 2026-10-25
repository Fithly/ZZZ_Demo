using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Excel 导表后的背包发物测试入口
/// 
/// 用法：
/// 
/// 先确保 Excel 已导出 Json 到 Resources/Configs
/// 
/// 场景里挂一个这个组件
/// 
/// Start 时自动初始化数据库
/// 
/// 然后把配置中的物品发到背包里，方便测试 UI / 装备 / 强化
/// </summary>
public class BackpackItemGrantTestEntry : MonoBehaviour
{
    [Serializable]
    public class GrantItemData
    {
        public int ItemId;
        public int Count = 1;
    }

    [Header("Execute")]
    [SerializeField] private bool executeOnStart = true;

    [Header("Save Option")]
    [SerializeField] private bool recreateSaveBeforeGrant = false;
    [SerializeField] private bool clearBackpackBeforeGrant = false;  // 清空当前背包
    [SerializeField] private string saveFileName = null;

    // “如果背包已有该物品则跳过”开关，避免每次运行重复发放
    [SerializeField] private bool skipGrantIfItemAlreadyExists = true;

    [Header("Grant Items")]
    [SerializeField] private List<GrantItemData> grantItems = new List<GrantItemData>();

    private SaveGameService saveGameService;
    private InventoryService inventoryService;

    private void Start()
    {
        if (!executeOnStart)
            return;

        ExecuteGrant();
    }

    public void ExecuteGrant()
    {
        // 先初始化物品数据库（会读取 Excel 导出的 Json）
        GameItemDatabase.Initialize();

        saveGameService = new SaveGameService();
        inventoryService = new InventoryService(GameItemDatabase.Repository);

        GameSaveData saveData;

        if (recreateSaveBeforeGrant)
        {
            // 没有指定测试文件时，创建当前账号的新存档。
            // 指定 saveFileName 时，仍然走原来的测试文件逻辑。
            saveData = string.IsNullOrEmpty(saveFileName)
                ? saveGameService.CreateNewSaveForCurrentAccount()
                : saveGameService.CreateNewSave();
        }
        else
        {
            // 没有指定测试文件时，读取当前账号存档。
            // 指定 saveFileName 时，读取指定测试存档。
            saveData = string.IsNullOrEmpty(saveFileName)
                ? saveGameService.LoadOrCreateForCurrentAccount()
                : saveGameService.LoadOrCreate(saveFileName);
        }

        if (saveData == null)
        {
            Debug.LogError("[BackpackItemGrantTestEntry] 数据为空.");
            return;
        }

        if (clearBackpackBeforeGrant)
        {
            // 测试时可选清空当前背包，只清物品，不改容量
            ClearBackpack(saveData.BackpackData);
        }

        int successCount = 0;

        for (int i = 0; i < grantItems.Count; i++)
        {
            GrantItemData data = grantItems[i];
            if (data == null)
                continue;

            if (data.ItemId <= 0 || data.Count <= 0)
                continue;

            // 如果背包里已经存在该物品，则跳过，避免每次运行重复发放
            if (skipGrantIfItemAlreadyExists)
            {
                int ownedCount = inventoryService.GetItemCount(saveData.BackpackData, data.ItemId).TotalCount;
                if (ownedCount > 0)
                {
                    Debug.Log($"[BackpackItemGrantTestEntry] 跳过发放. 物品已存在. ItemId={data.ItemId}, Owned={ownedCount}");
                    continue;
                }
            }

            bool success = inventoryService.AddItemWithAutoExpand(
                saveData.BackpackData,
                data.ItemId,
                data.Count);

            if (success)
            {
                successCount++;
                Debug.Log($"[BackpackItemGrantTestEntry] 发物成功. ItemId={data.ItemId}, Count={data.Count}");
            }
            else
            {
                Debug.LogWarning($"[BackpackItemGrantTestEntry] 发物失败. ItemId={data.ItemId}, Count={data.Count}");
            }
        }

        // 默认发到当前账号存档；如果 Inspector 指定了 saveFileName，则仍然保存到指定测试文件。
        if (string.IsNullOrEmpty(saveFileName))
            saveGameService.SaveForCurrentAccount(saveData);
        else
            saveGameService.Save(saveData, saveFileName);

        // 发物保存后，主动通知当前场景里的 GameUIEntry 重载存档并刷新 UI
        GameUIEntry uiEntry = FindObjectOfType<GameUIEntry>();
        if (uiEntry != null)
            uiEntry.ReloadAndRefreshFromSave(saveFileName);

        Debug.Log($"[BackpackItemGrantTestEntry] 执行完毕. 成功次数={successCount}, 物品总数={grantItems.Count}");
    }

    private void ClearBackpack(InventoryData inventoryData)
    {
        if (inventoryData == null || inventoryData.Slots == null)
            return;

        for (int i = 0; i < inventoryData.Slots.Count; i++)
        {
            if (inventoryData.Slots[i] == null)
                continue;

            inventoryData.Slots[i].Item = null;
        }
    }
}