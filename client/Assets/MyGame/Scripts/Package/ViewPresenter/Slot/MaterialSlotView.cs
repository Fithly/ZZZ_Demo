using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MaterialSlotView : InventorySlotView
{
    public override void Bind(
        InventoryDisplayItem displayItem,
        Sprite iconSprite,
        bool selected,
        Action<InventorySlotView, InventoryDisplayItem> onClick)
    {
        base.Bind(displayItem, iconSprite, selected, onClick);

        // ===== 修改：先安全取值，避免空槽位直接空引用 =====
        ItemInstanceData instance = displayItem != null ? displayItem.ItemInstance : null;
        bool hasItem = displayItem != null && instance != null;

        if (contentText != null)
        {
            if (hasItem && displayItem.ItemDefinition is MaterialDefinition)
            {
                // 显示正确物品数量
                if (displayItem.ItemInstance.Count > 0)
                {
                    contentText.gameObject.SetActive(true);
                    contentText.text = displayItem.ItemInstance.Count.ToString();
                }
                else
                {
                    contentText.gameObject.SetActive(false);
                    contentText.text = string.Empty;
                }
            }
            else
            {
                contentText.gameObject.SetActive(false);
                contentText.text = string.Empty;
            }
        }
    }
}
