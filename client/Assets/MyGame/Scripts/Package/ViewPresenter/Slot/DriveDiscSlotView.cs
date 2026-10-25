using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DriveDiscSlotView : InventorySlotView
{
    [SerializeField] private List<GameObject> driNumber;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private GameObject discardableIcon;

    public override void Bind(
        InventoryDisplayItem displayItem,
        Sprite iconSprite,
        bool selected,
        Action<InventorySlotView, InventoryDisplayItem> onClick)
    {
        base.Bind(displayItem, iconSprite, selected, onClick);

        ItemInstanceData instance = displayItem != null ? displayItem.ItemInstance : null;
        bool hasItem = instance != null;

        HideAll();

        // 标记
        if (hasItem)
        {
            if (instance.IsLocked && lockIcon != null)
                lockIcon.SetActive(true);

            if (instance.IsMarkedDiscardable && discardableIcon != null)
                discardableIcon.SetActive(true);
        }

        // 编号
        if (driNumber != null)
        {
            for (int i = 0; i < driNumber.Count; i++)
            {
                if (driNumber[i] != null)
                    driNumber[i].SetActive(false);
            }

            var driveDiscDef = displayItem != null ? displayItem.ItemDefinition as DriveDiscDefinition : null;
            if (driveDiscDef != null)
            {
                // 只显示对应编号
                int discNumberIndex = driveDiscDef.DiscNumber - 1;
                if (discNumberIndex >= 0 && discNumberIndex < driNumber.Count && driNumber[discNumberIndex] != null)
                    driNumber[discNumberIndex].SetActive(true);
            }
        }

        // 只在有驱动盘实例时显示等级
        if (contentText != null)
        {
            // =增加空判断，避免空槽位时直接访问 displayItem.ItemDefinition=
            if (hasItem && displayItem != null && displayItem.ItemDefinition is DriveDiscDefinition)
            {
                // 显示正确装备等级
                contentText.gameObject.SetActive(true);
                contentText.text = "等级" + instance.Level.ToString();
            }
            else
            {
                contentText.gameObject.SetActive(false);
                contentText.text = string.Empty;
            }
        }
    }

    private void HideAll()
    {
        if (lockIcon != null)
            lockIcon.SetActive(false);

        if (discardableIcon != null)
            discardableIcon.SetActive(false);

        if (driNumber != null)
        {
            for (int i = 0; i < driNumber.Count; i++)
            {
                if (driNumber[i] != null)
                    driNumber[i].SetActive(false);
            }
        }
    }
}
