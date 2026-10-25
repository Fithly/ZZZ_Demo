using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WeaponSlotView : InventorySlotView
{
    [SerializeField] private GameObject starRoot;
    [SerializeField] private List<GameObject> weaponStar;
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

        if (hasItem)
        {
            if (instance.IsLocked && lockIcon != null)
                lockIcon.SetActive(true);

            if (instance.IsMarkedDiscardable && discardableIcon != null)
                discardableIcon.SetActive(true);
        }

        // ===== 修改：先安全判断 displayItem / ItemDefinition，避免空槽位时报错 =====
        WeaponDefinition weaponDef = displayItem != null ? displayItem.ItemDefinition as WeaponDefinition : null;
        bool isWeaponItem = weaponDef != null;

        // 星级
        if (starRoot != null)
            starRoot.SetActive(hasItem && isWeaponItem);

        if (weaponStar != null)
        {
            for (int i = 0; i < weaponStar.Count; i++)
            {
                if (weaponStar[i] != null)
                    weaponStar[i].SetActive(false);
            }

            if (weaponDef != null)
            {
                for (int i = 0; i < weaponStar.Count; i++)
                {
                    if (weaponStar[i] != null)
                        weaponStar[i].SetActive(i < weaponDef.StartNumber);
                }
            }
        }

        if (contentText != null)
        {
            // 显示正确武器等级
            if (hasItem && displayItem.ItemDefinition is WeaponDefinition)
            {
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

        if (starRoot != null)
            starRoot.SetActive(false);
    }
}
