using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 小背包里当前点击物品的 detail 面板
/// 负责显示：
///   
///   名字
///   
///   描述
///   
///   锁定状态
///   
///   可丢弃状态
/// </summary>
public class EquipCandidateDetailPresenter : MonoBehaviour
{
    [Header("Weapon Detail")]
    [SerializeField] private GameObject weaponDetailRoot;                  // 武器面板
    [SerializeField] private TextMeshProUGUI weaponNameText;               // 武器名
    [SerializeField] private TextMeshProUGUI weaponFunctionName;           // 武器效果名
    [SerializeField] private TextMeshProUGUI weaponDescText;               // 武器详情
    [SerializeField] private TextMeshProUGUI weaponLevelText;              // 武器等级
    [SerializeField] private List<GameObject> weaponStar;                  // 武器星级
    [SerializeField] private TextMeshProUGUI weaponBaseAttribution;             // 武器基础属性
    [SerializeField] private TextMeshProUGUI weaponBaseAttributionValue;        // 武器基础属性值
    [SerializeField] private TextMeshProUGUI weaponAdvanceAttribution;          // 武器高级属性
    [SerializeField] private TextMeshProUGUI weaponAdvanceAttributionValue;     // 武器高级属性值

    [Header("DriveDisc Detail")]
    [SerializeField] private GameObject driveDiscDetailRoot;               // 驱动盘面板
    [SerializeField] private TextMeshProUGUI driveDiscNameText;            // 驱动盘名
    [SerializeField] private TextMeshProUGUI driveDiscFunctionName;        // 驱动盘效果名
    [SerializeField] private TextMeshProUGUI driveDiscDescText;            // 驱动盘详细
    [SerializeField] private TextMeshProUGUI driveDiscLevelText;           // 驱动盘等级
    [SerializeField] private TextMeshProUGUI driveDiscMainAttribution;               // 驱动盘主属性
    [SerializeField] private TextMeshProUGUI driveDiscMainAttributionValue;          // 驱动盘主属性值
    [SerializeField] private List<TextMeshProUGUI> driveDiscMinorAttribution;        // 驱动盘副属性
    [SerializeField] private List<TextMeshProUGUI> driveDiscMinorAttributionValue;   // 驱动盘副属性值


    private InventoryDisplayItem currentItem;
    public InventoryDisplayItem CurrentItem => currentItem;

    public void ShowItem(InventoryDisplayItem item)
    {
        currentItem = item;

        // 先把所有都隐藏一遍
        HideAll();
        ClearTexts(); // 切换候选物品前清空旧内容

        if (item == null || item.ItemDefinition == null || item.ItemInstance == null)
            return;

        switch (item.ItemDefinition.ItemType)
        {
            case ItemType.Weapon:
                ShowWeapon(item);
                break;

            case ItemType.DriveDisc:
                ShowDriveDisc(item);
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

        var weaponDef = item.ItemDefinition as WeaponDefinition;
        if (weaponDef == null)
            return;

        // 等级
        if (weaponLevelText != null)
            weaponLevelText.text = item.ItemInstance.Level.ToString();

        // 星级
        if (weaponStar != null)
        {
            for (int i = 0; i < weaponStar.Count; i++)
            {
                // 只开启对应数量的星级
                if (weaponStar[i] != null)
                    weaponStar[i].SetActive(i < weaponDef.StartNumber);
            }
        }

        // 武器效果
        if(weaponFunctionName != null)
            weaponFunctionName.text = weaponDef.FunctionName;

        // 属性
        if (weaponBaseAttribution != null)
            weaponBaseAttribution.text = weaponDef.BaseAttributionKey;

        if (weaponAdvanceAttribution != null)
            weaponAdvanceAttribution.text = weaponDef.AdvanceAttributionKey;

        if (weaponBaseAttributionValue != null)
            weaponBaseAttributionValue.text = FormatAttributeValue(weaponDef.BaseAttributionKey, GetMergedValue(item, weaponDef.BaseAttributionKey));

        if (weaponAdvanceAttributionValue != null)
            weaponAdvanceAttributionValue.text = FormatAttributeValue(weaponDef.AdvanceAttributionKey, GetMergedValue(item, weaponDef.AdvanceAttributionKey));
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
        if (driveDiscDef == null)
            return;

        if(driveDiscLevelText !=  null)
            driveDiscLevelText.text = item.ItemInstance.Level.ToString();

        // 驱动盘效果
        if (driveDiscFunctionName != null)
            driveDiscFunctionName.text = driveDiscDef.FunctionName;

        // 主属性
        if (driveDiscMainAttribution != null)
            driveDiscMainAttribution.text = driveDiscDef.MainAttributionKey;

        if (driveDiscMainAttributionValue != null)
            driveDiscMainAttributionValue.text = FormatAttributeValue(driveDiscDef.MainAttributionKey, GetMergedValue(item, driveDiscDef.MainAttributionKey));

        // 副属性
        if (driveDiscMinorAttribution != null && driveDiscMinorAttributionValue != null)
        {
            int minorTextCount = driveDiscMinorAttribution.Count;
            int minorValueCount = driveDiscMinorAttributionValue.Count;
            int defMinorCount = driveDiscDef.MinorAttributionKey != null ? driveDiscDef.MinorAttributionKey.Count : 0;
            int bindCount = Mathf.Min(minorTextCount, minorValueCount);

            for (int i = 0; i < bindCount; i++)
            {
                string key = i < defMinorCount ? driveDiscDef.MinorAttributionKey[i] : string.Empty;

                if (string.IsNullOrEmpty(key))
                {
                    if (driveDiscMinorAttribution[i] != null)
                        driveDiscMinorAttribution[i].text = string.Empty;

                    if (driveDiscMinorAttributionValue[i] != null)
                        driveDiscMinorAttributionValue[i].text = string.Empty;

                    continue;
                }

                string formattedValue = FormatAttributeValue(key, GetMergedValue(item, key));
                int rollCount = CountRolledKey(item.ItemInstance.RolledMinorAttributeKeys, key);

                if (driveDiscMinorAttribution[i] != null)
                    driveDiscMinorAttribution[i].text = $"{key}{(rollCount > 0 ? $" +{rollCount}" : "")}";

                if (driveDiscMinorAttributionValue[i] != null)
                    driveDiscMinorAttributionValue[i].text = formattedValue;
            }
        }
    }

    private float GetMergedValue(InventoryDisplayItem item, string key)
    {
        if (item == null || string.IsNullOrEmpty(key))
            return 0f;

        float baseValue = 0f;
        if (item.ItemDefinition != null)
        {
            if (item.ItemDefinition is WeaponDefinition weaponDef && weaponDef.Attributes != null)
                baseValue = GetValue(weaponDef.Attributes, key);

            if (item.ItemDefinition is DriveDiscDefinition driveDiscDef && driveDiscDef.Attributes != null)
                baseValue = GetValue(driveDiscDef.Attributes, key);
        }

        float extraValue = item.ItemInstance != null
            ? GetValue(item.ItemInstance.ExtraAttributes, key)
            : 0f;

        return baseValue + extraValue;
    }

    private float GetValue(List<ItemAttributeData> attributes, string key)
    {
        if (attributes == null || string.IsNullOrEmpty(key))
            return 0f;

        for (int i = 0; i < attributes.Count; i++)
        {
            if (attributes[i].Key == key)
                return attributes[i].Value;
        }

        return 0f;
    }

    private int CountRolledKey(List<string> keys, string targetKey)
    {
        if (keys == null || string.IsNullOrEmpty(targetKey))
            return 0;

        int count = 0;

        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] == targetKey)
                count++;
        }

        return count;
    }

    private string FormatAttributeValue(string key, float value)
    {
        if (key == AttributeKeyUtility.CritRate || key == AttributeKeyUtility.CritDamage)
            return value.ToString("P0");  // 格式化为百分比

        return value.ToString("0.##");  // 其他数值保留两位小数
    }

    // ===== 修改：新增统一清文本逻辑，避免类型切换残留 =====
    private void ClearTexts()
    {
        if (weaponNameText != null) weaponNameText.text = string.Empty;
        if (weaponFunctionName != null) weaponFunctionName.text = string.Empty;
        if (weaponDescText != null) weaponDescText.text = string.Empty;
        if (weaponLevelText != null) weaponLevelText.text = string.Empty;
        if (weaponBaseAttribution != null) weaponBaseAttribution.text = string.Empty;
        if (weaponBaseAttributionValue != null) weaponBaseAttributionValue.text = string.Empty;
        if (weaponAdvanceAttribution != null) weaponAdvanceAttribution.text = string.Empty;
        if (weaponAdvanceAttributionValue != null) weaponAdvanceAttributionValue.text = string.Empty;

        if (weaponStar != null)
        {
            for (int i = 0; i < weaponStar.Count; i++)
            {
                if (weaponStar[i] != null)
                    weaponStar[i].SetActive(false);
            }
        }

        if (driveDiscNameText != null) driveDiscNameText.text = string.Empty;
        if (driveDiscFunctionName != null) driveDiscFunctionName.text = string.Empty;
        if (driveDiscDescText != null) driveDiscDescText.text = string.Empty;
        if (driveDiscLevelText != null) driveDiscLevelText.text = string.Empty;
        if (driveDiscMainAttribution != null) driveDiscMainAttribution.text = string.Empty;
        if (driveDiscMainAttributionValue != null) driveDiscMainAttributionValue.text = string.Empty;

        if (driveDiscMinorAttribution != null)
        {
            for (int i = 0; i < driveDiscMinorAttribution.Count; i++)
            {
                if (driveDiscMinorAttribution[i] != null)
                    driveDiscMinorAttribution[i].text = string.Empty;
            }
        }

        if (driveDiscMinorAttributionValue != null)
        {
            for (int i = 0; i < driveDiscMinorAttributionValue.Count; i++)
            {
                if (driveDiscMinorAttributionValue[i] != null)
                    driveDiscMinorAttributionValue[i].text = string.Empty;
            }
        }
    }

    private void HideAll()
    {
        if (weaponDetailRoot != null) weaponDetailRoot.SetActive(false);
        if (driveDiscDetailRoot != null) driveDiscDetailRoot.SetActive(false);
    }
}