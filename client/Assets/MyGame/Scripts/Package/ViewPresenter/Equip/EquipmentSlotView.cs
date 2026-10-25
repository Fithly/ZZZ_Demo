using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlotView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject selectedHighlightObject;
    //[SerializeField] private GameObject emptyStateObject;
    //[SerializeField] private TextMeshProUGUI slotNameText;

    private EquipmentSlotContext boundContext;
    private Action<EquipmentSlotContext> onClick;

    public void Bind(
        EquipmentSlotContext context,
        Sprite icon,
        bool selected,
        Action<EquipmentSlotContext> onClick)
    {
        boundContext = context;
        this.onClick = onClick;

        // 只有有效装备数据才算“槽位有物品”
        bool hasItem = context != null && IsValidEquippedItem(context.EquippedItem);

        //if (slotNameText != null)
        //    slotNameText.text = context != null ? context.DisplayName : string.Empty;

        if (iconImage != null)
        {
            iconImage.enabled = hasItem;
            iconImage.sprite = icon;
        }

        //if (emptyStateObject != null)
        //    emptyStateObject.SetActive(!hasItem);

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

    private void HandleClick()
    {
        onClick?.Invoke(boundContext);
        SetSelected(true);
    }

    public void ShowIcon()
    {
        if(iconImage != null)
            iconImage.gameObject.SetActive(true);
    }

    public void HideIcon()
    {
        if (iconImage != null)
            iconImage.gameObject.SetActive(false);
    }

    // 统一判断“装备数据是否有效”的方法
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