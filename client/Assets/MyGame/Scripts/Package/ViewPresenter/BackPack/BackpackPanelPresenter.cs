using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包界面表现层
/// 
/// 负责：
/// 
/// 菜单高亮显示
/// 
/// Grid 初始化与刷新
/// 
/// Detail 显示与清空
/// 
/// 不负责筛选逻辑与分类状态切换
/// </summary>
public class BackpackPanelPresenter : MonoBehaviour
{
    // 菜单高亮
    [Header("Category Button Highlights")]
    [SerializeField] private GameObject weaponTabSelectedObject;
    [SerializeField] private GameObject driveDiscTabSelectedObject;
    [SerializeField] private GameObject materialTabSelectedObject;

    // 槽预制体
    [Header("Slot Prefabs")]
    [SerializeField] private InventorySlotView weaponSlotPrefab;
    [SerializeField] private InventorySlotView driveDiscSlotPrefab;
    [SerializeField] private InventorySlotView materialSlotPrefab;

    // 展示
    [Header("Presenters")]
    [SerializeField] private InventoryGridPresenter gridPresenter;
    [SerializeField] private ItemDetailPresenter detailPresenter;

    private IItemIconProvider iconProvider;
    private Action<InventoryDisplayItem> onItemSelected;

    public void Initialize(
        IItemIconProvider iconProvider,
        Action<InventoryDisplayItem> onItemSelected)
    {
        this.iconProvider = iconProvider;
        this.onItemSelected = onItemSelected;
    }

    // 刷新整个背包表现
    public void RefreshView(
        BackpackCategoryType currentCategory,
        List<InventoryDisplayItem> items)
    {
        RefreshTabSelectedState(currentCategory);

        InventorySlotView prefab = GetCurrentSlotPrefab(currentCategory);
        if (gridPresenter != null)
        {
            gridPresenter.Initialize(prefab, iconProvider, HandleItemSelected);
            gridPresenter.Refresh(items);
        }

        // 分类切换后统一清空 detail
        if (detailPresenter != null)
            detailPresenter.ShowItem(null);
    }

    public void ClearDetail()
    {
        if (detailPresenter != null)
            detailPresenter.ShowItem(null);
    }

    private InventorySlotView GetCurrentSlotPrefab(BackpackCategoryType currentCategory)
    {
        switch (currentCategory)
        {
            case BackpackCategoryType.Weapon:
                return weaponSlotPrefab;

            case BackpackCategoryType.DriveDisc:
                return driveDiscSlotPrefab;

            case BackpackCategoryType.Material:
                return materialSlotPrefab;

            default:
                return weaponSlotPrefab;
        }
    }

    private void RefreshTabSelectedState(BackpackCategoryType currentCategory)
    {
        if (weaponTabSelectedObject != null)
            weaponTabSelectedObject.SetActive(currentCategory == BackpackCategoryType.Weapon);

        if (driveDiscTabSelectedObject != null)
            driveDiscTabSelectedObject.SetActive(currentCategory == BackpackCategoryType.DriveDisc);

        if (materialTabSelectedObject != null)
            materialTabSelectedObject.SetActive(currentCategory == BackpackCategoryType.Material);
    }

    private void HandleItemSelected(InventoryDisplayItem item)
    {
        if (detailPresenter != null)
            detailPresenter.ShowItem(item);

        onItemSelected?.Invoke(item);
    }
}