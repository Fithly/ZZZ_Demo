using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 打开/关闭面板
/// 小背包 4 列候选列表
/// 候选物品 detail
/// 可丢弃 / 锁定
/// 拆解
/// 单个卸下
/// 一键卸下全部驱动盘
/// 强化入口
/// 装备候选物品
/// </summary>
public class EquipSelectionPanelController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Candidate Grid")]
    [SerializeField] private InventoryGridPresenter candidateGridPresenter;

    [Header("Detail")]
    [SerializeField] private EquipCandidateDetailPresenter detailPresenter;

    [Header("Buttons")]
    [SerializeField] private Button returnButton;                    // 返回按钮
    [SerializeField] private Button markDiscardableButton;           // 标记删除按钮
    [SerializeField] private Button lockButton;                      // 标记锁定按钮
    [SerializeField] private Button dismantleButton;                 // 回收按钮
    [SerializeField] private Button unequipCurrentButton;            // 卸载当前槽位按钮
    [SerializeField] private Button unequipAllDriveDiscButton;       // 卸载所有驱动盘按钮
    [SerializeField] private Button strengthenButton;                // 强化按钮
    [SerializeField] private Button equipButton;                     // 装备按钮
    [SerializeField] private Button switchButton;                    // 替换按钮

    // 槽预制体
    [Header("Slot Prefabs")]
    [SerializeField] private InventorySlotView weaponSlotPrefab;     // 武器槽
    [SerializeField] private InventorySlotView driveDiscSlotPrefab;  // 驱动盘槽

    private EquipmentSlotContext selectEquipSlot;                    // 被选中的槽位

    private GameSaveData saveData;
    private SaveGameService saveGameService;
    private EquipmentService equipmentService;
    private EquipmentCandidateFilterService candidateFilterService;
    private EquipmentBatchOperationService batchOperationService;
    private ItemMarkService itemMarkService;
    private IItemIconProvider iconProvider;

    private EquipmentSlotContext currentSlotContext;
    private InventoryDisplayItem currentCandidateItem;

    private Action onDataChanged;
    private Action<EquipmentSlotContext, InventoryDisplayItem> onStrengthenRequested;

    // 关闭附加面板时的回调，用于取消装备槽高亮
    private Action onPanelClosed;

    public void Initialize(
        GameSaveData saveData,
        SaveGameService saveGameService,
        EquipmentService equipmentService,
        EquipmentCandidateFilterService candidateFilterService,
        EquipmentBatchOperationService batchOperationService,
        ItemMarkService itemMarkService,
        EquipmentSlotContext equipmentSlotContext,
        IItemIconProvider iconProvider,
        Action onDataChanged,
        Action<EquipmentSlotContext, InventoryDisplayItem> onStrengthenRequested,
        Action onPanelClosed)
    {
        this.saveData = saveData;
        this.saveGameService = saveGameService;
        this.equipmentService = equipmentService;
        this.candidateFilterService = candidateFilterService;
        this.batchOperationService = batchOperationService;
        this.itemMarkService = itemMarkService;
        this.selectEquipSlot = equipmentSlotContext;
        this.iconProvider = iconProvider;
        this.onDataChanged = onDataChanged;
        this.onStrengthenRequested = onStrengthenRequested;
        this.onPanelClosed = onPanelClosed;

        RefreshSlotView();
        BindButtons();
        HidePanel();
    }

    // 打开附加面板
    public void Open(EquipmentSlotContext slotContext)
    {
        // 每次打开小背包时，都重新从 saveData 同步当前槽位的最新装备引用
        currentSlotContext = BuildLatestSlotContext(slotContext);
        currentCandidateItem = null;
        selectEquipSlot = currentSlotContext;

        if (root != null)
            root.SetActive(true);

        RefreshSlotView();
        RefreshCandidateList();
        RefreshButtonStates();
    }

    // 外部刷新入口，供“物品数据变化后实时刷新当前打开的小背包”使用
    public void RefreshExternal()
    {
        if (root == null || !root.activeSelf)
            return;

        if (currentSlotContext == null)
            return;

        string selectedInstanceId = currentCandidateItem != null && currentCandidateItem.ItemInstance != null
            ? currentCandidateItem.ItemInstance.InstanceId
            : string.Empty;

        int selectedSourceSlotIndex = currentCandidateItem != null
            ? currentCandidateItem.SourceSlotIndex
            : -1;

        currentSlotContext = BuildLatestSlotContext(currentSlotContext);
        selectEquipSlot = currentSlotContext;

        RefreshSlotView();
        RefreshCandidateListPreserveSelection(selectedInstanceId, selectedSourceSlotIndex);
        RefreshButtonStates();
    }

    // 刷新候补列表预选
    private void RefreshCandidateListPreserveSelection(string selectedInstanceId, int selectedSourceSlotIndex)
    {
        if (currentSlotContext == null)
        {
            candidateGridPresenter.Refresh(new List<InventoryDisplayItem>());
            return;
        }

        var query = new EquipmentCandidateFilterQuery
        {
            SlotCategory = currentSlotContext.SlotCategory,
            DriveDiscSlotType = currentSlotContext.DriveDiscSlotType
        };

        // 根据筛选获取物品列表
        var items = candidateFilterService.BuildCandidates(saveData.BackpackData, query);

        // 找到数据模型
        InventoryDisplayItem equippedDisplayItem = FindEquippedDisplayItem(items, currentSlotContext);

        if (equippedDisplayItem != null)
        {
            items.Remove(equippedDisplayItem);
            items.Insert(0, equippedDisplayItem);
        }

        InventoryDisplayItem selectedItem = FindSelectedItem(items, selectedInstanceId, selectedSourceSlotIndex);

        if (selectedItem != null)
        {
            currentCandidateItem = selectedItem;

            if (candidateGridPresenter != null)
                candidateGridPresenter.SetSelectedSourceSlotIndex(selectedItem.SourceSlotIndex);
        }
        else if (equippedDisplayItem != null)
        {
            currentCandidateItem = equippedDisplayItem;

            if (candidateGridPresenter != null)
                candidateGridPresenter.SetSelectedSourceSlotIndex(equippedDisplayItem.SourceSlotIndex);
        }
        else
        {
            currentCandidateItem = null;

            if (candidateGridPresenter != null)
                candidateGridPresenter.SetSelectedSourceSlotIndex(-1);
        }

        candidateGridPresenter.Refresh(items);

        if (detailPresenter != null)
            detailPresenter.ShowItem(currentCandidateItem);
    }

    private InventoryDisplayItem FindSelectedItem(List<InventoryDisplayItem> items, string selectedInstanceId, int selectedSourceSlotIndex)
    {
        if (items == null)
            return null;

        for (int i = 0; i < items.Count; i++)
        {
            InventoryDisplayItem item = items[i];
            if (item == null || item.ItemInstance == null)
                continue;

            if (!string.IsNullOrEmpty(selectedInstanceId) && item.ItemInstance.InstanceId == selectedInstanceId)
                return item;

            if (selectedSourceSlotIndex >= 0 && item.SourceSlotIndex == selectedSourceSlotIndex)
                return item;
        }

        return null;
    }

    private EquipmentSlotContext BuildLatestSlotContext(EquipmentSlotContext slotContext)
    {
        if (slotContext == null)
            return null;

        var latestContext = new EquipmentSlotContext
        {
            SlotCategory = slotContext.SlotCategory,
            DriveDiscSlotType = slotContext.DriveDiscSlotType,
            EquippedItem = null
        };

        if (saveData == null)
            return latestContext;

        if (slotContext.IsWeaponSlot)
        {
            latestContext.EquippedItem = saveData.EquippedWeapon;
            return latestContext;
        }

        if (slotContext.IsDriveDiscSlot && saveData.DriveDiscEquipData != null)
        {
            switch (slotContext.DriveDiscSlotType)
            {
                case DriveDiscSlotType.One:
                    latestContext.EquippedItem = saveData.DriveDiscEquipData.One;
                    break;
                case DriveDiscSlotType.Two:
                    latestContext.EquippedItem = saveData.DriveDiscEquipData.Two;
                    break;
                case DriveDiscSlotType.Three:
                    latestContext.EquippedItem = saveData.DriveDiscEquipData.Three;
                    break;
                case DriveDiscSlotType.Four:
                    latestContext.EquippedItem = saveData.DriveDiscEquipData.Four;
                    break;
                case DriveDiscSlotType.Five:
                    latestContext.EquippedItem = saveData.DriveDiscEquipData.Five;
                    break;
                case DriveDiscSlotType.Six:
                    latestContext.EquippedItem = saveData.DriveDiscEquipData.Six;
                    break;
            }
        }

        return latestContext;
    }

    // 隐藏附加面板
    public void HidePanel()
    {
        currentSlotContext = null;
        currentCandidateItem = null;

        if (root != null)
            root.SetActive(false);

        if (detailPresenter != null)
            detailPresenter.ShowItem(null);

        // 统一在面板隐藏时通知外层取消槽位高亮
        onPanelClosed?.Invoke();
    }

    // 刷新slot
    private void RefreshSlotView()
    {
        InventorySlotView prefab = GetCurrentSlotPrefab();
        candidateGridPresenter.Initialize(prefab, iconProvider, HandleCandidateSelected);
    }

    // 获得当前需要显示的Slot预制体
    private InventorySlotView GetCurrentSlotPrefab()
    {
        if (selectEquipSlot != null)
        {
            if (selectEquipSlot.IsWeaponSlot)
            {
                return weaponSlotPrefab;
            }

            if (selectEquipSlot.IsDriveDiscSlot)
            {
                return driveDiscSlotPrefab;
            }
        }
        return null;
    }

    // 刷新小背包列表
    private void RefreshCandidateList()
    {
        if (currentSlotContext == null)
        {
            candidateGridPresenter.Refresh(new List<InventoryDisplayItem>());
            return;
        }
        
        // 根据点击目标来进行筛选
        var query = new EquipmentCandidateFilterQuery
        {
            SlotCategory = currentSlotContext.SlotCategory,
            DriveDiscSlotType = currentSlotContext.DriveDiscSlotType
        };

        var items = candidateFilterService.BuildCandidates(saveData.BackpackData, query);

        // 如果当前槽位已有装备，则把“已装备物品”移到候选列表第一个，并默认选中显示
        InventoryDisplayItem equippedDisplayItem = FindEquippedDisplayItem(items, currentSlotContext);

        if (equippedDisplayItem != null)
        {
            items.Remove(equippedDisplayItem);
            items.Insert(0, equippedDisplayItem);

            currentCandidateItem = equippedDisplayItem;

            if (candidateGridPresenter != null)
                candidateGridPresenter.SetSelectedSourceSlotIndex(equippedDisplayItem.SourceSlotIndex);
        }
        else
        {
            currentCandidateItem = null;

            if (candidateGridPresenter != null)
                candidateGridPresenter.SetSelectedSourceSlotIndex(-1);
        }

        candidateGridPresenter.Refresh(items);

        if (detailPresenter != null)
            detailPresenter.ShowItem(currentCandidateItem);
    }

    // 查找“当前已装备物品在候选列表中的显示项”的方法
    private InventoryDisplayItem FindEquippedDisplayItem(List<InventoryDisplayItem> items, EquipmentSlotContext slotContext)
    {
        if (items == null || slotContext == null || slotContext.EquippedItem == null)
            return null;

        for (int i = 0; i < items.Count; i++)
        {
            InventoryDisplayItem item = items[i];
            if (item == null || item.ItemInstance == null)
                continue;

            if (IsSameInstance(item.ItemInstance, slotContext.EquippedItem))
                return item;
        }

        return null;
    }

    // 统一实例比较方法
    private bool IsSameInstance(ItemInstanceData a, ItemInstanceData b)
    {
        if (a == null || b == null)
            return false;

        if (!string.IsNullOrEmpty(a.InstanceId) && !string.IsNullOrEmpty(b.InstanceId))
            return a.InstanceId == b.InstanceId;

        return ReferenceEquals(a, b);
    }

    // 处理小背包点击事件 显示detail面板
    private void HandleCandidateSelected(InventoryDisplayItem item)
    {
        currentCandidateItem = item;

        if (detailPresenter != null)
            detailPresenter.ShowItem(item);

        RefreshButtonStates();
    }

    // 绑定按键事件
    private void BindButtons()
    {
        if (returnButton != null)
        {
            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(HidePanel);
        }

        if (markDiscardableButton != null)
        {
            markDiscardableButton.onClick.RemoveAllListeners();
            markDiscardableButton.onClick.AddListener(ToggleDiscardable);
        }

        if (lockButton != null)
        {
            lockButton.onClick.RemoveAllListeners();
            lockButton.onClick.AddListener(ToggleLock);
        }

        if (dismantleButton != null)
        {
            dismantleButton.onClick.RemoveAllListeners();
            dismantleButton.onClick.AddListener(DismantleMarkedItems);
        }

        if (unequipCurrentButton != null)
        {
            unequipCurrentButton.onClick.RemoveAllListeners();
            unequipCurrentButton.onClick.AddListener(UnequipCurrentSlot);
        }

        if (unequipAllDriveDiscButton != null)
        {
            unequipAllDriveDiscButton.onClick.RemoveAllListeners();
            unequipAllDriveDiscButton.onClick.AddListener(UnequipAllDriveDiscs);
        }

        if (strengthenButton != null)
        {
            strengthenButton.onClick.RemoveAllListeners();
            strengthenButton.onClick.AddListener(OpenStrengthenPanel);
        }

        if (equipButton != null)
        {
            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(EquipCurrentCandidate);
        }

        if (switchButton != null)
        {
            switchButton.onClick.RemoveAllListeners();
            switchButton.onClick.AddListener(ReplaceCurrentCandidate);
        }
    }

    // 刷新按键显示状态
    private void RefreshButtonStates()
    {
        bool isDriveDiscSlot = currentSlotContext != null && currentSlotContext.IsDriveDiscSlot;
        bool hasEquippedItem = currentSlotContext != null && currentSlotContext.EquippedItem != null;
        bool hasCandidateItem = currentCandidateItem != null && currentCandidateItem.ItemInstance != null;
        bool candidateIsEquippedItem = IsCurrentCandidateEquippedItem();

        // 如果当前点击的是驱动盘槽位 则显示“一键卸下”的按键
        if (unequipAllDriveDiscButton != null)
            unequipAllDriveDiscButton.gameObject.SetActive(isDriveDiscSlot);

        // ===== 修改：统一控制“装备 / 卸下 / 替换”三个按钮 =====
        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(!hasEquippedItem);
            equipButton.interactable = !hasEquippedItem && hasCandidateItem;
        }

        // 装备槽有装备 && 没有选中候选背包物品 或者 选中候选物品为装备物品
        if (unequipCurrentButton != null)
            unequipCurrentButton.gameObject.SetActive(hasEquippedItem && (!hasCandidateItem || candidateIsEquippedItem));

        // 装备槽有装备 && 选中候选背包物品 && 选中候选物品不为装备物品
        if (switchButton != null)
            switchButton.gameObject.SetActive(hasEquippedItem && hasCandidateItem && !candidateIsEquippedItem);
    }

    // 统一判断“候选物品是不是当前槽位已装备物品”的方法
    private bool IsCurrentCandidateEquippedItem()
    {
        if (currentSlotContext == null || currentSlotContext.EquippedItem == null || currentCandidateItem == null || currentCandidateItem.ItemInstance == null)
            return false;

        return IsSameInstance(currentSlotContext.EquippedItem, currentCandidateItem.ItemInstance);
    }

    // 切换丢弃状态
    private void ToggleDiscardable()
    {
        if (currentCandidateItem?.ItemInstance == null)
            return;

        itemMarkService.ToggleDiscardable(currentCandidateItem.ItemInstance);
        saveGameService.SaveForCurrentAccount(saveData);
        // 刷新detail
        if (detailPresenter != null)
            detailPresenter.ShowItem(currentCandidateItem);
        onDataChanged?.Invoke();
    }

    // 切换锁定状态
    private void ToggleLock()
    {
        if (currentCandidateItem?.ItemInstance == null)
            return;

        itemMarkService.ToggleLock(currentCandidateItem.ItemInstance);
        saveGameService.SaveForCurrentAccount(saveData);

        if (detailPresenter != null)
            detailPresenter.ShowItem(currentCandidateItem);
        onDataChanged?.Invoke();
    }

    // 回收标记物品
    private void DismantleMarkedItems()
    {
        var result = batchOperationService.DismantleMarkedDiscardableItems(saveData);
        if (!result.Success)
            return;

        saveGameService.SaveForCurrentAccount(saveData);
        currentCandidateItem = null;
        RefreshCandidateList();
        if (detailPresenter != null)
            detailPresenter.ShowItem(null);
        onDataChanged?.Invoke();
    }

    // 卸载当前槽位装备
    private void UnequipCurrentSlot()
    {
        if (currentSlotContext == null)
            return;

        if (currentSlotContext.IsWeaponSlot)
        {
            var result = equipmentService.UnequipWeapon(saveData);
            if (!result.Success)
                return;
        }
        else if (currentSlotContext.IsDriveDiscSlot)
        {
            var result = equipmentService.UnequipDriveDisc(saveData, currentSlotContext.DriveDiscSlotType);
            if (!result.Success)
                return;
        }

        saveGameService.SaveForCurrentAccount(saveData);

        // 卸下后，当前槽位已无装备，detail 清空，小背包重刷
        currentCandidateItem = null;
        currentSlotContext.EquippedItem = null;

        RefreshCandidateList();

        if (detailPresenter != null)
            detailPresenter.ShowItem(null);

        RefreshButtonStates();
        onDataChanged?.Invoke();
    }

    // 一键卸载所有驱动盘
    private void UnequipAllDriveDiscs()
    {
        var result = batchOperationService.UnequipAllDriveDiscs(saveData);
        if (!result.Success)
            return;

        saveGameService.SaveForCurrentAccount(saveData);
        currentCandidateItem = null;

        RefreshCandidateList();

        if (detailPresenter != null)
            detailPresenter.ShowItem(null);

        RefreshButtonStates();
        onDataChanged?.Invoke();
    }

    // 装备当前选中的装备
    private void EquipCurrentCandidate()
    {
        if (currentCandidateItem == null)
            return;

        var result = equipmentService.EquipFromBackpack(saveData, currentCandidateItem.SourceSlotIndex);
        if (!result.Success)
            return;

        saveGameService.SaveForCurrentAccount(saveData);

        // 装备后刷新当前槽位上下文，并重刷列表，让已装备物品回到第一个并保持选中
        currentSlotContext.EquippedItem = currentCandidateItem.ItemInstance;

        RefreshCandidateList();
        RefreshButtonStates();
        onDataChanged?.Invoke();
    }

    // “替换”按钮逻辑，本质上仍然是装备选中的另一个候选物品
    private void ReplaceCurrentCandidate()
    {
        EquipCurrentCandidate();
    }

    // 打开强化界面
    private void OpenStrengthenPanel()
    {
        // 强化入口允许“强化当前槽位装备”或“强化当前候选物品”
        if (currentSlotContext == null)
            return;

        onStrengthenRequested?.Invoke(currentSlotContext, currentCandidateItem);
    }
}