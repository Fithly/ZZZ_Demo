using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包格子网格展示核心
/// 
/// 动态生成 slot
/// 
/// 刷新 slot
/// 
/// 管理当前选中
/// 
/// 点击后把物品传给外部 detail 逻辑
/// </summary>
public class InventoryGridPresenter : MonoBehaviour
{
    [SerializeField] private Transform slotRoot;

    // slot列表
    private readonly List<InventorySlotView> slotViews = new List<InventorySlotView>();

    [Header("LayOut Config")]
    public const int DefaultCapacity = 45;
    public const int RowSlotCount = 9;
    public const int ExpandCountPerTime = RowSlotCount;

    // 预制体
    private InventorySlotView slotPrefab;
    // 获取图标
    private IItemIconProvider iconProvider;
    
    // 物品数据源列表
    private List<InventoryDisplayItem> currentItems = new List<InventoryDisplayItem>();
    // 选中物品的数据源索引
    private int selectedSourceSlotIndex = -1;

    // 被选中回调
    private Action<InventoryDisplayItem> onItemSelected;

    // 初始化
    public void Initialize(
        InventorySlotView slotPrefab,
        IItemIconProvider iconProvider,
        Action<InventoryDisplayItem> onItemSelected)
    {
        // 如果预制体类型变了，重建整个 grid，避免武器页残留材料格子
        if (this.slotPrefab != slotPrefab)
        {
            this.slotPrefab = slotPrefab;
            RebuildAllSlots();
        }
        else
        {
            this.slotPrefab = slotPrefab;
        }

        this.iconProvider = iconProvider;
        this.onItemSelected = onItemSelected;
    }

    // 新增外部设置预选中物品的方法
    public void SetSelectedSourceSlotIndex(int sourceSlotIndex)
    {
        selectedSourceSlotIndex = sourceSlotIndex;
    }

    // 刷新视图
    public void Refresh(List<InventoryDisplayItem> displayItems)
    {
        currentItems = displayItems ?? new List<InventoryDisplayItem>();

        if (slotRoot == null || slotPrefab == null)
            return;

        // 无论当前物品数量多少，至少显示 DefaultCapacity 个格子
        // 如果物品数量超过 DefaultCapacity，则按整行补齐显示
        int requiredSlotCount = GetRequiredSlotCount(currentItems.Count);

        if (currentItems.Count <= 0)
        {
            ClearSelection();
            EnsureSlotCount(requiredSlotCount);
        }
        else
        {
            EnsureSlotCount(requiredSlotCount);
        }

        // 遍历显示出来的slot
        for (int i = 0; i < slotViews.Count; i++)
        {
            // 获取物品时 判断是否小于当前物品总数量
            InventoryDisplayItem item = i < currentItems.Count ? currentItems[i] : null;
            Sprite icon = null;
            bool selected = false;

            if (item != null)
            {
                // 获取图标
                if (iconProvider != null)
                    icon = iconProvider.GetIcon(item.ItemDefinition);

                // 是否被选中
                selected = item.SourceSlotIndex == selectedSourceSlotIndex;
            }

            // 绑定每个slot的物品数据
            slotViews[i].Bind(item, icon, selected, HandleSlotClicked);
        }
    }

    // 新增统一计算“当前至少应显示多少格”的方法
    public static int GetRequiredSlotCount(int itemCount)
    {
        int validItemCount = Mathf.Max(0, itemCount);

        // 至少显示默认容量
        int requiredCount = Mathf.Max(DefaultCapacity, validItemCount);

        // 按整行补齐(49格子一行9格 补5个)
        int remain = requiredCount % RowSlotCount;
        if (remain != 0)
            requiredCount += RowSlotCount - remain;

        return requiredCount;
    }

    // 取消选择
    public void ClearSelection()
    {
        selectedSourceSlotIndex = -1;

        for (int i = 0; i < slotViews.Count; i++)
        {
            slotViews[i].SetSelected(false);
        }
    }

    // 新增统一重建方法
    private void RebuildAllSlots()
    {
        for (int i = 0; i < slotViews.Count; i++)
        {
            if (slotViews[i] != null)
                Destroy(slotViews[i].gameObject);
        }

        slotViews.Clear();
        selectedSourceSlotIndex = -1;
    }

    // 确定slot数量
    private void EnsureSlotCount(int requiredCount)
    {
        if (slotRoot == null || slotPrefab == null)
            return;

        // 当前显示的slot数量小于需要显示的数量
        while (slotViews.Count < requiredCount)
        {
            // 生成新的slot
            var view = Instantiate(slotPrefab, slotRoot);
            slotViews.Add(view);
        }

        // 显示正确数量的slot
        for (int i = 0; i < slotViews.Count; i++)
        {
            if (slotViews[i] != null)
                slotViews[i].gameObject.SetActive(i < requiredCount);
        }
    }

    // 处理slot点击
    private void HandleSlotClicked(InventorySlotView clickedView, InventoryDisplayItem item)
    {
        // 点击空槽时，不触发 detail 刷新，也不改变当前选中
        if (item == null)
            return;

        // 被选中slot的源索引
        selectedSourceSlotIndex = item.SourceSlotIndex;

        // 找到对应槽位并高亮显示
        for (int i = 0; i < slotViews.Count; i++)
        {
            bool selected = currentItems.Count > i &&
                            currentItems[i] != null &&
                            currentItems[i].SourceSlotIndex == selectedSourceSlotIndex;

            if (slotViews[i] != null)
                slotViews[i].SetSelected(selected);
        }

        onItemSelected?.Invoke(item);
    }
}