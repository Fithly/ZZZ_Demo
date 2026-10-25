using System;
using System.IO;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个 slot 视图
/// 负责：
///  图标显示
///  数量显示
///  选中高亮 object 开关
///  保存当前绑定数据
///  点击回调
/// 不负责业务逻辑
/// </summary>
public class InventorySlotView : MonoBehaviour
{
    [SerializeField] protected Button button;
    [SerializeField] protected Image iconImage;
    [SerializeField] protected GameObject selectedHighlightObject;
    [SerializeField] protected TextMeshProUGUI contentText;

    [Header("Quality Frame")]
    [SerializeField] protected Image qualityFrameImage;

    protected InventoryDisplayItem boundItem;
    protected Action<InventorySlotView, InventoryDisplayItem> onClick;

    // 对Slot进行绑定
    public virtual void Bind(
        InventoryDisplayItem displayItem,
        Sprite iconSprite,
        bool selected,
        Action<InventorySlotView, InventoryDisplayItem> onClick)
    {
        boundItem = displayItem;
        this.onClick = onClick;

        bool hasItem = displayItem != null && displayItem.ItemInstance != null;

        // 显示Slot
        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(hasItem);
            iconImage.sprite = iconSprite;
        }

        RefreshQualityFrame(displayItem);

        // 选中状态
        if (selectedHighlightObject != null)
            selectedHighlightObject.SetActive(selected);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlightObject != null)
            selectedHighlightObject.SetActive(selected);
    }

    // 新增统一品质边框刷新逻辑
    protected virtual void RefreshQualityFrame(InventoryDisplayItem displayItem)
    {
        if (qualityFrameImage == null)
            return;

        bool hasItem = displayItem != null &&
                       displayItem.ItemInstance != null &&
                       displayItem.ItemDefinition != null;

        if (!hasItem)
        {
            qualityFrameImage.enabled = false;
            qualityFrameImage.sprite = null;
            qualityFrameImage.gameObject.SetActive(false);
            return;
        }

        Sprite frameSprite = GetQualityFrameSprite(displayItem.ItemDefinition.Quality);

        qualityFrameImage.gameObject.SetActive(true);
        qualityFrameImage.sprite = frameSprite;
        qualityFrameImage.enabled = frameSprite != null;
    }

    // 根据 ItemQuality 选择对应边框
    protected virtual Sprite GetQualityFrameSprite(ItemQuality quality)
    {
        switch (quality)
        {
            case ItemQuality.Last:
                return Resources.Load<Sprite>(QualityIconUtility.QualityLastIconPath);

            case ItemQuality.Low:
                return Resources.Load<Sprite>(QualityIconUtility.QualityLowIconPath);

            case ItemQuality.Middle:
                return Resources.Load<Sprite>(QualityIconUtility.QualityMidIconPath);

            case ItemQuality.High:
                return Resources.Load<Sprite>(QualityIconUtility.QualityHighIconPath);

            case ItemQuality.None:
            default:
                return null;
        }
    }

    private void HandleClick()
    {
        onClick?.Invoke(this, boundItem);
    }
}