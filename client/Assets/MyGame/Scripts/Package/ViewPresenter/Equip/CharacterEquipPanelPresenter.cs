using System;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 每个槽位一个 EquipmentSlotView
/// 
/// 点击槽位高亮
/// 
/// 打开附加小背包面板
/// 
/// 由附加面板处理装备/卸下/拆解/强化
/// </summary>
public class CharacterEquipPanelPresenter : MonoBehaviour
{
    // 槽位
    [SerializeField] private EquipmentSlotView weaponSlotView;
    [SerializeField] private EquipmentSlotView disc1SlotView;
    [SerializeField] private EquipmentSlotView disc2SlotView;
    [SerializeField] private EquipmentSlotView disc3SlotView;
    [SerializeField] private EquipmentSlotView disc4SlotView;
    [SerializeField] private EquipmentSlotView disc5SlotView;
    [SerializeField] private EquipmentSlotView disc6SlotView;

    // 执行控制器
    [SerializeField] private EquipSelectionPanelController selectionPanelController;

    private GameSaveData saveData;
    private SaveGameService saveGameService;
    private EquipmentService equipmentService;
    private ItemDefinitionRepository repository;
    private IItemIconProvider iconProvider;

    // 选中的槽位
    private EquipmentSlotContext currentSelectedSlot;

    public void Initialize(
        GameSaveData saveData,
        SaveGameService saveGameService,
        EquipmentService equipmentService,
        ItemDefinitionRepository repository,
        IItemIconProvider iconProvider,

        EquipmentCandidateFilterService candidateFilterService,
        EquipmentBatchOperationService batchOperationService,
        ItemMarkService itemMarkService,
        Action onEquipmentChanged,
        Action<EquipmentSlotContext, InventoryDisplayItem> onStrengthenRequested)
    {
        this.saveData = saveData;
        this.saveGameService = saveGameService;
        this.equipmentService = equipmentService;
        this.repository = repository;
        this.iconProvider = iconProvider;

        if (selectionPanelController != null)
        {
            selectionPanelController.Initialize(
                saveData,
                saveGameService,
                equipmentService,
                candidateFilterService,
                batchOperationService,
                itemMarkService,
                currentSelectedSlot,
                iconProvider,
                () =>
                {
                    Refresh();
                    onEquipmentChanged?.Invoke();
                },
                onStrengthenRequested,
                HandleSelectionPanelClosed);
        }

        Refresh();
    }

    public void Refresh()
    {
        BindWeaponSlot();
        BindDriveDiscSlot(disc1SlotView, DriveDiscSlotType.One);
        BindDriveDiscSlot(disc2SlotView, DriveDiscSlotType.Two);
        BindDriveDiscSlot(disc3SlotView, DriveDiscSlotType.Three);
        BindDriveDiscSlot(disc4SlotView, DriveDiscSlotType.Four);
        BindDriveDiscSlot(disc5SlotView, DriveDiscSlotType.Five);
        BindDriveDiscSlot(disc6SlotView, DriveDiscSlotType.Six);

        // 装备区刷新时，如果小背包当前正打开，就同步刷新当前打开的小背包
        if (selectionPanelController != null)
            selectionPanelController.RefreshExternal();
    }

    private void BindWeaponSlot()
    {
        if (weaponSlotView == null)
            return;

        // 只有有效装备数据才允许显示在装备槽
        ItemInstanceData equippedWeapon = IsValidEquippedItem(saveData != null ? saveData.EquippedWeapon : null)
            ? saveData.EquippedWeapon
            : null;

        var context = new EquipmentSlotContext
        {
            SlotCategory = EquipmentSlotCategoryType.Weapon,
            DriveDiscSlotType = DriveDiscSlotType.None,
            // 这里必须使用校验后的 equippedWeapon
            EquippedItem = equippedWeapon
        };

        var def = context.EquippedItem != null && repository != null
            ? repository.Get(context.EquippedItem.ItemId)
            : null;

        var icon = def != null && iconProvider != null
            ? iconProvider.GetIcon(def)
            : null;

        if (icon == null)
            weaponSlotView.HideIcon();
        else
            weaponSlotView.ShowIcon();

        weaponSlotView.Bind(context, icon, IsSelected(context), HandleSlotClicked);
    }

    private void BindDriveDiscSlot(EquipmentSlotView slotView, DriveDiscSlotType slotType)
    {
        if (slotView == null)
            return;

        // 只有有效装备数据才允许显示在装备槽
        var rawEquippedItem = GetDriveDisc(slotType);
        var equippedItem = IsValidEquippedItem(rawEquippedItem) ? rawEquippedItem : null;

        var context = new EquipmentSlotContext
        {
            SlotCategory = EquipmentSlotCategoryType.DriveDisc,
            DriveDiscSlotType = slotType,
            EquippedItem = equippedItem
        };

        var def = equippedItem != null && repository != null
            ? repository.Get(equippedItem.ItemId)
            : null;

        var icon = def != null && iconProvider != null
            ? iconProvider.GetIcon(def)
            : null;

        if (icon == null)
            slotView.HideIcon();
        else
            slotView.ShowIcon();

        slotView.Bind(context, icon, IsSelected(context), HandleSlotClicked);
    }

    private ItemInstanceData GetDriveDisc(DriveDiscSlotType slotType)
    {
        if (saveData == null || saveData.DriveDiscEquipData == null)
            return null;

        switch (slotType)
        {
            case DriveDiscSlotType.One: return saveData.DriveDiscEquipData.One;
            case DriveDiscSlotType.Two: return saveData.DriveDiscEquipData.Two;
            case DriveDiscSlotType.Three: return saveData.DriveDiscEquipData.Three;
            case DriveDiscSlotType.Four: return saveData.DriveDiscEquipData.Four;
            case DriveDiscSlotType.Five: return saveData.DriveDiscEquipData.Five;
            case DriveDiscSlotType.Six: return saveData.DriveDiscEquipData.Six;
            default: return null;
        }
    }

    private void HandleSlotClicked(EquipmentSlotContext context)
    {
        currentSelectedSlot = context;
        Refresh();
        
        if (selectionPanelController != null)
        {
            selectionPanelController.Open(context);
        }
    }

    // 附加面板关闭后的统一取消高亮逻辑
    private void HandleSelectionPanelClosed()
    {
        currentSelectedSlot = null;
        Refresh();
    }

    private bool IsSelected(EquipmentSlotContext context)
    {
        if (currentSelectedSlot == null || context == null)
            return false;

        return currentSelectedSlot.SlotCategory == context.SlotCategory &&
               currentSelectedSlot.DriveDiscSlotType == context.DriveDiscSlotType;
    }

    // 统一判断“装备槽数据是否有效”的方法
    private bool IsValidEquippedItem(ItemInstanceData item)
    {
        if (item == null)
            return false;

        if (item.ItemId <= 0)
            return false;

        if (item.Count <= 0)
            return false;

        if (string.IsNullOrEmpty(item.InstanceId))
            return false;

        return true;
    }
}