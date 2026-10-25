using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 详情面板总控制器
/// 
/// 根据物品类型切换不同 detail 面板
/// 
/// 先把显示逻辑和按钮入口留出来
/// 
/// 后面材料使用、装备、卸下都从这里接
/// </summary>
public class ItemDetailPresenter : MonoBehaviour
{
    [Header("Common")]
    [SerializeField] private GameObject emptyRoot;              // 空面板

    [Header("Weapon Detail")]
    [SerializeField] private GameObject weaponDetailRoot;       // 武器面板
    [SerializeField] private TextMeshPro weaponNameText;               // 武器名
    [SerializeField] private TextMeshPro baseAttribute;               // 基础属性名
    [SerializeField] private TextMeshPro weaponDescText;               // 武器详情

    [Header("DriveDisc Detail")]
    [SerializeField] private GameObject driveDiscDetailRoot;    // 驱动盘面板
    [SerializeField] private TextMeshPro driveDiscNameText;            // 驱动盘名
    [SerializeField] private TextMeshPro driveDiscDescText;            // 驱动盘详细
    [SerializeField] private TextMeshPro driveDiscSetText;             // 驱动盘套装名
    [SerializeField] private TextMeshPro driveDiscSlotText;            // 驱动盘编号

    [Header("Material Detail")]
    [SerializeField] private GameObject materialDetailRoot;     // 材料面板
    [SerializeField] private TextMeshPro materialNameText;             // 材料名
    [SerializeField] private TextMeshPro materialDescText;             // 材料详细
    [SerializeField] private TextMeshPro materialCountText;            // 材料数量

    private InventoryDisplayItem currentItem;

    public InventoryDisplayItem CurrentItem => currentItem;

    public void ShowItem(InventoryDisplayItem item)
    {
        currentItem = item;

        // 先把所有都隐藏一遍
        HideAll();
        ClearTexts(); // 切换物品前先清空，避免旧文本残留

        if (item == null || item.ItemDefinition == null || item.ItemInstance == null)
        {
            if (emptyRoot != null)
                emptyRoot.SetActive(true);
            return;
        }

        switch (item.ItemDefinition.ItemType)
        {
            case ItemType.Weapon:
                ShowWeapon(item);
                break;

            case ItemType.DriveDisc:
                ShowDriveDisc(item);
                break;

            case ItemType.Material:
                ShowMaterial(item);
                break;

            default:
                if (emptyRoot != null)
                    emptyRoot.SetActive(true);
                break;
        }
    }

    private void ShowWeapon(InventoryDisplayItem item)
    {
        if (weaponDetailRoot != null)
            weaponDetailRoot.SetActive(true);

        if (weaponNameText != null)
            weaponNameText.text = item.ItemDefinition.Name;

        if (weaponDescText != null)
            weaponDescText.text = item.ItemDefinition.Description;
    }

    private void ShowDriveDisc(InventoryDisplayItem item)
    {
        if (driveDiscDetailRoot != null)
            driveDiscDetailRoot.SetActive(true);

        if (driveDiscNameText != null)
            driveDiscNameText.text = item.ItemDefinition.Name;

        if (driveDiscDescText != null)
            driveDiscDescText.text = item.ItemDefinition.Description;

        var driveDiscDef = item.ItemDefinition as DriveDiscDefinition;
        if (driveDiscDef != null)
        {
            if (driveDiscSetText != null)
                driveDiscSetText.text = driveDiscDef.SetType.ToString();

            if (driveDiscSlotText != null)
                driveDiscSlotText.text = driveDiscDef.SlotType.ToString();
        }
    }

    private void ShowMaterial(InventoryDisplayItem item)
    {
        if (materialDetailRoot != null)
            materialDetailRoot.SetActive(true);

        if (materialNameText != null)
            materialNameText.text = item.ItemDefinition.Name;

        if (materialDescText != null)
            materialDescText.text = item.ItemDefinition.Description;

        if (materialCountText != null)
            materialCountText.text = item.ItemInstance.Count.ToString();
    }

    // 新增统一清文本方法，减少不同类型切换时显示残留
    private void ClearTexts()
    {
        if (weaponNameText != null) weaponNameText.text = string.Empty;
        if (weaponDescText != null) weaponDescText.text = string.Empty;

        if (driveDiscNameText != null) driveDiscNameText.text = string.Empty;
        if (driveDiscDescText != null) driveDiscDescText.text = string.Empty;
        if (driveDiscSetText != null) driveDiscSetText.text = string.Empty;
        if (driveDiscSlotText != null) driveDiscSlotText.text = string.Empty;

        if (materialNameText != null) materialNameText.text = string.Empty;
        if (materialDescText != null) materialDescText.text = string.Empty;
        if (materialCountText != null) materialCountText.text = string.Empty;
    }

    private void HideAll()
    {
        if (emptyRoot != null) emptyRoot.SetActive(false);
        if (weaponDetailRoot != null) weaponDetailRoot.SetActive(false);
        if (driveDiscDetailRoot != null) driveDiscDetailRoot.SetActive(false);
        if (materialDetailRoot != null) materialDetailRoot.SetActive(false);
    }
}