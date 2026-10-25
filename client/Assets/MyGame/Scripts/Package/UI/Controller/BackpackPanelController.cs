using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包界面总控制器
/// 
/// 管三分类按钮
/// 
/// 组装筛选条件
/// 
/// 刷新背包数据
/// 
/// 具体显示交给 BackpackPanelPresenter
/// </summary>
public class BackpackPanelController : UIPanelBase
{
    [Header("Presenter")]
    [SerializeField] private BackpackPanelPresenter backpackPanelPresenter;

    private GameSaveData saveData;
    private InventoryFilterService filterService;
    private IItemIconProvider iconProvider;

    private BackpackCategoryType currentCategory = BackpackCategoryType.Weapon;
    private DriveDiscSetType currentDriveDiscSetType = DriveDiscSetType.None;
    private bool enableDriveDiscSetFilter = false;

    public void Initialize(
        GameSaveData saveData,
        InventoryFilterService filterService,
        IItemIconProvider iconProvider)
    {
        this.saveData = saveData;
        this.filterService = filterService;
        this.iconProvider = iconProvider;

        // 初始化 Presenter 层
        if (backpackPanelPresenter != null)
            backpackPanelPresenter.Initialize(iconProvider, HandleItemSelected);

        RefreshCurrentView();
    }

    // 每次打开背包面板时，强制按最新 saveData 刷新一次
    public override void Show()
    {
        base.Show();
        RefreshCurrentView();
    }

    public void ShowWeaponCategory()
    {
        currentCategory = BackpackCategoryType.Weapon;
        enableDriveDiscSetFilter = false;
        RefreshCurrentView();
    }

    public void ShowDriveDiscCategory()
    {
        currentCategory = BackpackCategoryType.DriveDisc;
        enableDriveDiscSetFilter = false;           // 切到驱动盘大类时默认先关闭套装筛选
        currentDriveDiscSetType = DriveDiscSetType.None;
        RefreshCurrentView();
    }

    public void ShowMaterialCategory()
    {
        currentCategory = BackpackCategoryType.Material;
        enableDriveDiscSetFilter = false;
        RefreshCurrentView();
    }

    public void SetDriveDiscSetFilter(DriveDiscSetType setType)
    {
        currentCategory = BackpackCategoryType.DriveDisc;
        currentDriveDiscSetType = setType;
        enableDriveDiscSetFilter = setType != DriveDiscSetType.None;
        RefreshCurrentView();
    }

    // 刷新面板
    private void RefreshCurrentView()
    {
        if (saveData == null || filterService == null || backpackPanelPresenter == null)
            return;

        // 组装筛选条件
        var query = new InventoryFilterQuery
        {
            Category = currentCategory,
            EnableDriveDiscSetFilter = enableDriveDiscSetFilter,
            DriveDiscSetType = currentDriveDiscSetType
        };

        // 从总背包进行筛选，可以分三大类，也可以筛选套装
        List<InventoryDisplayItem> items = filterService.BuildDisplayItems(saveData.BackpackData, query);

        // ===== 修改：具体显示逻辑转交给 Presenter 层 =====
        backpackPanelPresenter.RefreshView(currentCategory, items);
    }

    public void RefreshExternal()
    {
        RefreshCurrentView();
    }

    // ===== 修改：预留背包内物品点击后的控制层扩展入口 =====
    private void HandleItemSelected(InventoryDisplayItem item)
    {
        // 当前背包点击物品后暂无额外控制逻辑
        // 先由 BackpackPanelPresenter 内部处理 detail 显示
    }
}