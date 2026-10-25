using System;
using UnityEngine;

/// <summary>
/// 强化总面板控制器
///
/// 接收从装备附加面板传来的槽位和物品。
/// 根据物品类型切换到武器强化或驱动盘强化。
/// 
/// 打开强化面板
/// 如果传入的是已选候选物品，就强化候选物品
/// 如果没有候选物品，则优先强化当前装备槽位上的装备
/// 根据物品类型打开武器或驱动盘强化子面板
/// 给子面板注入存档、保存服务、强化服务、刷新回调
/// 关闭时统一隐藏 root 和两个子面板
/// </summary>
public class EquipStrengthenPanelController : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private WeaponStrengthenPresenter weaponStrengthenPresenter;
    [SerializeField] private DriveDiscStrengthenPresenter driveDiscStrengthenPresenter;

    private GameSaveData saveData;
    private SaveGameService saveGameService;
    private ItemDefinitionRepository repository;
    private WeaponStrengthenService weaponStrengthenService;
    private DriveDiscStrengthenService driveDiscStrengthenService;

    private Action onDataChanged;

    private EquipmentSlotContext currentSlotContext;
    private InventoryDisplayItem currentItem;

    public void Initialize(
        GameSaveData saveData,
        SaveGameService saveGameService,
        ItemDefinitionRepository repository,
        WeaponStrengthenService weaponStrengthenService,
        DriveDiscStrengthenService driveDiscStrengthenService,
        IItemIconProvider iconProvider,
        Action onDataChanged)
    {
        this.saveData = saveData;
        this.saveGameService = saveGameService;
        this.repository = repository;
        this.weaponStrengthenService = weaponStrengthenService;
        this.driveDiscStrengthenService = driveDiscStrengthenService;
        this.onDataChanged = onDataChanged;

        if (weaponStrengthenPresenter != null)
        {
            weaponStrengthenPresenter.Initialize(
                saveData,
                saveGameService,
                weaponStrengthenService,
                iconProvider,
                HandleStrengthenDataChanged,
                Close);
        }

        if (driveDiscStrengthenPresenter != null)
        {
            driveDiscStrengthenPresenter.Initialize(
                saveData,
                saveGameService,
                driveDiscStrengthenService,
                iconProvider,
                HandleStrengthenDataChanged,
                Close);
        }

        HideAll();
    }

    public void Open(EquipmentSlotContext slotContext, InventoryDisplayItem item)
    {
        currentSlotContext = slotContext;
        currentItem = ResolveTargetItem(slotContext, item);

        HideAll();

        if (currentItem == null || currentItem.ItemDefinition == null || currentItem.ItemInstance == null)
            return;

        if (root != null)
            root.SetActive(true);

        switch (currentItem.ItemDefinition.ItemType)
        {
            case ItemType.Weapon:
                if (weaponStrengthenPresenter != null)
                {
                    weaponStrengthenPresenter.gameObject.SetActive(true);
                    weaponStrengthenPresenter.Bind(slotContext, currentItem);
                }
                break;

            case ItemType.DriveDisc:
                if (driveDiscStrengthenPresenter != null)
                {
                    driveDiscStrengthenPresenter.gameObject.SetActive(true);
                    driveDiscStrengthenPresenter.Bind(slotContext, currentItem);
                }
                break;

            default:
                // ===== 修改：兜底隐藏，防止未知类型停留在打开状态 =====
                HideAll();
                break;
        }
    }

    public void Close()
    {
        currentSlotContext = null;
        currentItem = null;
        HideAll();
    }

    // 解析实际要强化的目标物品：如果槽位上有已装备物品，则优先使用该装备生成显示物品；否则返回传入的物品
    private InventoryDisplayItem ResolveTargetItem(EquipmentSlotContext slotContext, InventoryDisplayItem inputItem)
    {
        // 检查槽位及其装备是否存在
        if (slotContext != null && slotContext.EquippedItem != null && repository != null)
        {
            // 通过仓库获取该装备的物品定义
            ItemDefinition def = repository.Get(slotContext.EquippedItem.ItemId);

            // 包裹槽位上的装备实例和定义
            if (def != null)
            {
                return new InventoryDisplayItem
                {
                    SourceSlotIndex = -1,
                    SourceSlotData = null,
                    ItemInstance = slotContext.EquippedItem,
                    ItemDefinition = def
                };
            }
        }

        // 如果槽位上没有装备，则使用调用者传入的物品
        return inputItem;
    }

    private void HandleStrengthenDataChanged()
    {
        onDataChanged?.Invoke();
    }

    private void HideAll()
    {
        if (root != null)
            root.SetActive(false);

        if (weaponStrengthenPresenter != null)
            weaponStrengthenPresenter.gameObject.SetActive(false);

        if (driveDiscStrengthenPresenter != null)
            driveDiscStrengthenPresenter.gameObject.SetActive(false);
    }
}