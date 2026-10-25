using UnityEngine;

/// <summary>
/// Lua 发物测试入口
/// 
/// 用法：
/// 1. 场景中挂这个组件
/// 2. 勾选 executeOnStart
/// 3. 修改 Demo/LuaDemoConfig.lua 中的 TestBackpackGrant_001
/// 4. 运行后观察背包变化
/// </summary>
public class LuaBackpackItemGrantTestEntry : MonoBehaviour
{
    [Header("Execute")]
    [SerializeField] private bool executeOnStart = false;

    private SaveGameService saveGameService;
    private InventoryService inventoryService;
    private LuaDemoBridge luaDemoBridge;

    private void Start()
    {
        if (!executeOnStart)
            return;

        ExecuteLuaGrant();
    }

    public void ExecuteLuaGrant()
    {
        GameItemDatabase.Initialize();

        saveGameService = new SaveGameService();
        inventoryService = new InventoryService(GameItemDatabase.Repository);

        GameSaveData saveData = saveGameService.LoadOrCreateForCurrentAccount();

        luaDemoBridge = new LuaDemoBridge();
        luaDemoBridge.Initialize(
            saveData,
            saveGameService,
            inventoryService);

        // 具体发什么物品，不在 Inspector 配，而是在 Lua 里配。
        luaDemoBridge.CallLuaFunction("OnTestBackpackGrant");

        GameUIEntry uiEntry = FindObjectOfType<GameUIEntry>();
        if (uiEntry != null)
            uiEntry.ReloadAndRefreshFromSave();
    }
}