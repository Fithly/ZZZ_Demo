using System;
using UnityEngine;

/// <summary>
/// UI 层总入口
/// 
/// 初始化服务
/// 
/// 读取存档
/// 
/// 初始化各个 Presenter / Controller
/// 
/// 把刷新链路串起来
/// </summary>
public class GameUIEntry : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private BackpackPanelController backpackPanelController;
    [SerializeField] private CharacterLevelUpPanelController characterLevelUpPanelController;
    [SerializeField] private EquipStrengthenPanelController equipStrengthenPanelController;

    [SerializeField] private CharacterBasePanelPresenter characterBasePanelPresenter;
    [SerializeField] private CharacterEquipPanelPresenter characterEquipPanelPresenter;
    [SerializeField] private RewardButton rewardButton;

    private LuaDemoBridge luaDemoBridge;
    private SaveGameService saveGameService;
    private InventoryService inventoryService;
    private EquipmentService equipmentService;
    private CharacterAttributeService attributeService;
    private InventoryFilterService filterService;
    private ItemIconService itemIconService;

    private IItemIconProvider iconProvider;
    private EquipmentCandidateFilterService candidateFilterService;
    private EquipmentBatchOperationService batchOperationService;
    private ItemMarkService itemMarkService;

    private CharacterLevelUpService characterLevelUpService;
    private WeaponStrengthenService weaponStrengthenService;
    private DriveDiscStrengthenService driveDiscStrengthenService;

    private GameSaveData saveData;
    private PlayerBaseData playerBaseData;
    private PlayerAttributeData playerAttributeData;

    private void Awake()
    {
        InitializeDatabaseAndServices();

        LoadSaveData();

        // 初始化 Lua Demo 桥，并执行登录奖励逻辑。
        // 首次进入 UI 时，执行登录奖励。
        InitializeLuaDemoBridge();
        rewardButton.Initialize(luaDemoBridge);

        InitializePresentersAndControllers();

        if (uiManager != null)
            uiManager.OpenBackpack();
    }

    // 初始化 Lua Demo 桥接层
    private void InitializeLuaDemoBridge()
    {
        luaDemoBridge = new LuaDemoBridge();

        luaDemoBridge.Initialize(
            saveData,
            saveGameService,
            inventoryService);
    }

    // 拆分初始化流程，让总入口职责更清晰
    private void InitializeDatabaseAndServices()
    {
        // 先初始化 Json 注册后的数据库，再初始化后续服务
        GameItemDatabase.Initialize();

        saveGameService = new SaveGameService();
        inventoryService = new InventoryService(GameItemDatabase.Repository);
        equipmentService = new EquipmentService(GameItemDatabase.Repository, inventoryService);
        attributeService = new CharacterAttributeService(GameItemDatabase.Repository);
        filterService = new InventoryFilterService(GameItemDatabase.Repository);
        itemIconService = new ItemIconService();

        characterLevelUpService = new CharacterLevelUpService(inventoryService);
        weaponStrengthenService = new WeaponStrengthenService(inventoryService);
        driveDiscStrengthenService = new DriveDiscStrengthenService(inventoryService);

        iconProvider = itemIconService;
        candidateFilterService = new EquipmentCandidateFilterService(GameItemDatabase.Repository);
        batchOperationService = new EquipmentBatchOperationService(equipmentService, inventoryService, GameItemDatabase.Repository);
        itemMarkService = new ItemMarkService();
    }

    // 单独封装存档读取
    private void LoadSaveData()
    {
        saveData = saveGameService.LoadOrCreateForCurrentAccount();
        playerBaseData = saveData.PlayerBaseData;
        playerAttributeData = saveData.PlayerAttributeData;
    }

    // 单独封装 UI 注入流程，便于后面接 Json 注册入口
    private void InitializePresentersAndControllers()
    {
        InitializeBackpackPanel();
        InitializeCharacterBasePanel();
        InitializeCharacterEquipPanel();
        InitializeCharacterLevelUpPanel();
        InitializeEquipStrengthenPanel();
    }

    // 拆成独立初始化方法，便于重载存档后全部重绑到最新 saveData
    private void InitializeBackpackPanel()
    {
        if (backpackPanelController != null)
            backpackPanelController.Initialize(saveData, filterService, itemIconService);
    }

    private void InitializeCharacterBasePanel()
    {
        if (characterBasePanelPresenter != null)
        {
            characterBasePanelPresenter.Initialize(HandleOpenCharacterLevelUpPanel);
            characterBasePanelPresenter.Refresh(saveData, attributeService);
        }
    }

    private void InitializeCharacterEquipPanel()
    {
        if (characterEquipPanelPresenter != null)
        {
            characterEquipPanelPresenter.Initialize(
                saveData,
                saveGameService,
                equipmentService,
                GameItemDatabase.Repository,
                iconProvider,
                candidateFilterService,
                batchOperationService,
                itemMarkService,
                HandleEquipmentChanged,
                HandleStrengthenRequested
            );
        }
    }

    private void InitializeCharacterLevelUpPanel()
    {
        if (characterLevelUpPanelController != null)
        {
            characterLevelUpPanelController.Initialize(
                saveData,
                saveGameService,
                playerBaseData,
                playerAttributeData,
                characterLevelUpService,
                HandleCharacterChanged);
        }
    }

    private void InitializeEquipStrengthenPanel()
    {
        if (equipStrengthenPanelController != null)
        {
            equipStrengthenPanelController.Initialize(
                saveData,
                saveGameService,
                GameItemDatabase.Repository,
                weaponStrengthenService,
                driveDiscStrengthenService,
                iconProvider,
                HandleEquipmentChanged);
        }
    }


    // “从磁盘重载并刷新 UI”的统一入口
    public void ReloadAndRefreshFromSave(string fileName = null)
    {
        // 如果外部明确传入 fileName，说明是在测试指定存档。
        // 如果没有传入 fileName，则默认重载当前账号存档。
        saveData = string.IsNullOrEmpty(fileName)
            ? saveGameService.LoadOrCreateForCurrentAccount()
            : saveGameService.LoadOrCreate(fileName);

        playerBaseData = saveData.PlayerBaseData;
        playerAttributeData = saveData.PlayerAttributeData;

        // 存档切换后，Lua Bridge 也要重新绑定最新 saveData。
        // 重载存档只是重新绑定 LuaBridge，不重复执行登录奖励。
        InitializeLuaDemoBridge();

        // 重载存档后，RewardButton 必须绑定新的 LuaDemoBridge
        if (rewardButton != null)
            rewardButton.Initialize(luaDemoBridge);

        // 把所有依赖 saveData 的面板一起重绑
        InitializeBackpackPanel();
        InitializeCharacterBasePanel();
        InitializeCharacterEquipPanel();
        InitializeCharacterLevelUpPanel();
        InitializeEquipStrengthenPanel();
    }

    private void HandleOpenCharacterLevelUpPanel()
    {
        if (characterLevelUpPanelController != null)
            characterLevelUpPanelController.Open(saveData.PlayerBaseData, saveData.PlayerAttributeData);
    }

    private void HandleCharacterChanged()
    {
        playerBaseData = saveData.PlayerBaseData;
        playerAttributeData = saveData.PlayerAttributeData;

        if (characterBasePanelPresenter != null)
            characterBasePanelPresenter.Refresh(saveData, attributeService);
    }

    private void HandleEquipmentChanged()
    {
        if (characterBasePanelPresenter != null)
            characterBasePanelPresenter.Refresh(saveData, attributeService);

        if (characterEquipPanelPresenter != null)
            characterEquipPanelPresenter.Refresh();

        if (backpackPanelController != null)
            backpackPanelController.RefreshExternal();
    }

    private void HandleStrengthenRequested(EquipmentSlotContext slotContext, InventoryDisplayItem item)
    {
        if (equipStrengthenPanelController != null)
            equipStrengthenPanelController.Open(slotContext, item);
    }
}