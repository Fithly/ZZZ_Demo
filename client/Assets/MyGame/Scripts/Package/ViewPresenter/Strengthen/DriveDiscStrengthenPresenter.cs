using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 驱动盘强化面板
///
/// 驱动盘等级、经验、主属性成长、副属性强化结果都读 ItemInstanceData。
/// Definition 只负责静态配置。
/// 
/// 显示驱动盘名、描述、编号、等级、效果名
/// 显示主属性和四个副属性
/// 三种驱动盘经验材料添加
/// 三种材料减少
/// 自动添加
/// 全部清除
/// 点击升级后调用 DriveDiscStrengthenService.Apply
/// 到 4 / 8 / 12 / 15 级时随机强化副属性
/// 保存强化结果
/// 刷新主副属性显示
/// Slider 经验条按时间变化
/// </summary>
public class DriveDiscStrengthenPresenter : MonoBehaviour
{
    [Header("Common")]
    [SerializeField] private TextMeshProUGUI driveDiscNameText;
    //[SerializeField] private TextMeshProUGUI driveDiscFunctionNameText;
    //[SerializeField] private TextMeshProUGUI driveDiscDescText;
    [SerializeField] private TextMeshProUGUI driveDiscLevelText;
    [SerializeField] private TextMeshProUGUI previewLevelText;
    //[SerializeField] private TextMeshProUGUI driveDiscNumberText;

    private IItemIconProvider iconProvider;
    [SerializeField] private Image weaponIcon;

    [Header("Attributes")]
    [SerializeField] private TextMeshProUGUI driveDiscMainAttributionText;
    [SerializeField] private TextMeshProUGUI driveDiscMainAttributionValueText;
    [SerializeField] private List<TextMeshProUGUI> driveDiscMinorAttributionTexts;
    [SerializeField] private List<TextMeshProUGUI> driveDiscMinorAttributionValueTexts;

    [Header("Exp")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private StrengthenExpSliderAnimator expSliderAnimator;
    [SerializeField] private TextMeshProUGUI expText;
    //[SerializeField] private TextMeshProUGUI addedExpText;

    [Header("Low Material")]
    [SerializeField] private Button addLowButton;
    [SerializeField] private Button reduceLowButton;
    [SerializeField] private TextMeshProUGUI lowCountText;

    [Header("Mid Material")]
    [SerializeField] private Button addMidButton;
    [SerializeField] private Button reduceMidButton;
    [SerializeField] private TextMeshProUGUI midCountText;

    [Header("High Material")]
    [SerializeField] private Button addHighButton;
    [SerializeField] private Button reduceHighButton;
    [SerializeField] private TextMeshProUGUI highCountText;

    [Header("Buttons")]
    [SerializeField] private Button autoAddButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button confirmStrengthenButton;

    [Header("Result")]
    private string resultText = string.Empty;

    private GameSaveData saveData;
    private SaveGameService saveGameService;
    private DriveDiscStrengthenService driveDiscStrengthenService;

    private Action onDataChanged;
    private Action onCloseRequested;

    private EquipmentSlotContext currentSlotContext;
    private InventoryDisplayItem currentItem;

    private int selectedLowCount;
    private int selectedMidCount;
    private int selectedHighCount;

    // 记录“上一次点击后的预览等级/经验”
    private int lastPreviewLevel;
    private int lastPreviewExp;

    public void Initialize(
        GameSaveData saveData,
        SaveGameService saveGameService,
        DriveDiscStrengthenService driveDiscStrengthenService,
        IItemIconProvider iconProvider,
        Action onDataChanged,
        Action onCloseRequested)
    {
        this.saveData = saveData;
        this.saveGameService = saveGameService;
        this.driveDiscStrengthenService = driveDiscStrengthenService;
        this.onDataChanged = onDataChanged;
        this.onCloseRequested = onCloseRequested;
        this.iconProvider = iconProvider;

        if (expSliderAnimator != null && expSlider != null)
            expSliderAnimator.SetSlider(expSlider);

        BindButtons();
    }

    public void Bind(EquipmentSlotContext slotContext, InventoryDisplayItem item)
    {
        currentSlotContext = slotContext;
        currentItem = item;

        selectedLowCount = 0;
        selectedMidCount = 0;
        selectedHighCount = 0;

        ClearResult();
        RefreshView(true, false, false);
    }

    private void BindButtons()
    {
        if (addLowButton != null)
        {
            addLowButton.onClick.RemoveAllListeners();
            addLowButton.onClick.AddListener(() => ChangeMaterialCount(StrengthenMaterialIds.DriveDiscExpLow, 1));
        }

        if (reduceLowButton != null)
        {
            reduceLowButton.onClick.RemoveAllListeners();
            reduceLowButton.onClick.AddListener(() => ChangeMaterialCount(StrengthenMaterialIds.DriveDiscExpLow, -1));
        }

        if (addMidButton != null)
        {
            addMidButton.onClick.RemoveAllListeners();
            addMidButton.onClick.AddListener(() => ChangeMaterialCount(StrengthenMaterialIds.DriveDiscExpMid, 1));
        }

        if (reduceMidButton != null)
        {
            reduceMidButton.onClick.RemoveAllListeners();
            reduceMidButton.onClick.AddListener(() => ChangeMaterialCount(StrengthenMaterialIds.DriveDiscExpMid, -1));
        }

        if (addHighButton != null)
        {
            addHighButton.onClick.RemoveAllListeners();
            addHighButton.onClick.AddListener(() => ChangeMaterialCount(StrengthenMaterialIds.DriveDiscExpHigh, 1));
        }

        if (reduceHighButton != null)
        {
            reduceHighButton.onClick.RemoveAllListeners();
            reduceHighButton.onClick.AddListener(() => ChangeMaterialCount(StrengthenMaterialIds.DriveDiscExpHigh, -1));
        }

        if (autoAddButton != null)
        {
            autoAddButton.onClick.RemoveAllListeners();
            autoAddButton.onClick.AddListener(HandleAutoAdd);
        }

        if (clearButton != null)
        {
            clearButton.onClick.RemoveAllListeners();
            clearButton.onClick.AddListener(HandleClear);
        }

        if (confirmStrengthenButton != null)
        {
            confirmStrengthenButton.onClick.RemoveAllListeners();
            confirmStrengthenButton.onClick.AddListener(HandleConfirmStrengthen);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => onCloseRequested?.Invoke());
        }
    }

    private void RefreshView(bool instantSlider, bool useVisualLevelUpLimit, bool updateSnapshot)
    {
        if (currentItem == null || currentItem.ItemDefinition == null || currentItem.ItemInstance == null)
            return;

        var driveDiscDef = currentItem.ItemDefinition as DriveDiscDefinition;
        var instance = currentItem.ItemInstance;

        if (driveDiscDef == null)
            return;

        var preview = driveDiscStrengthenService.Preview(
            saveData,
            instance,
            driveDiscDef,
            selectedLowCount,
            selectedMidCount,
            selectedHighCount);

        if (!preview.Success)
        {
            SetResult(preview.ErrorMessage);
            return;
        }

        Sprite icon = null;
        // 获取图标
        if (iconProvider != null)
            icon = iconProvider.GetIcon(driveDiscDef);

        if (weaponIcon != null)
            weaponIcon.sprite = icon;

        if (driveDiscNameText != null)
            driveDiscNameText.text = driveDiscDef.Name;

        //if (driveDiscFunctionNameText != null)
        //    driveDiscFunctionNameText.text = driveDiscDef.FunctionName;

        //if (driveDiscDescText != null)
        //    driveDiscDescText.text = driveDiscDef.Description;

        if (driveDiscLevelText != null)
            driveDiscLevelText.text = $"{instance.Level}";

        if (previewLevelText != null)
            previewLevelText.text = preview.PreviewLevel == instance.Level
                ? $"{instance.Level}"
                : $"{preview.PreviewLevel}";

        //if (driveDiscNumberText != null)
        //    driveDiscNumberText.text = driveDiscDef.DiscNumber.ToString();

        RefreshAttributes(driveDiscDef, instance);
        RefreshMaterialTexts(preview);
        RefreshExpTexts(preview);

        if (expSliderAnimator != null)
        {
            if (instantSlider)
            {
                expSliderAnimator.SetInstant(
                    instance.Level,
                    instance.CurrentLevelExp,
                    StrengthenBalanceConfig.DriveDiscMaxLevel,
                    StrengthenBalanceConfig.GetDriveDiscNeedExp);
            }
            else
            {
                expSliderAnimator.PlayPreviewAnimation(
                    preview.CurrentLevel,
                    preview.CurrentLevelExp,
                    preview.PreviewLevel,
                    preview.PreviewLevelExp,
                    StrengthenBalanceConfig.DriveDiscMaxLevel,
                    StrengthenBalanceConfig.GetDriveDiscNeedExp,
                    useVisualLevelUpLimit);
            }
        }

        if (updateSnapshot)
        {
            lastPreviewLevel = preview.PreviewLevel;
            lastPreviewExp = preview.PreviewLevelExp;
        }

        bool hasSelectedMaterial = selectedLowCount > 0 || selectedMidCount > 0 || selectedHighCount > 0;

        if (autoAddButton != null)
            autoAddButton.gameObject.SetActive(!hasSelectedMaterial);

        if (clearButton != null)
            clearButton.gameObject.SetActive(hasSelectedMaterial);

        if (reduceLowButton != null)
            reduceLowButton.gameObject.SetActive(selectedLowCount > 0);

        if (reduceMidButton != null)
            reduceMidButton.gameObject.SetActive(selectedMidCount > 0);

        if (reduceHighButton != null)
            reduceHighButton.gameObject.SetActive(selectedHighCount > 0);
    }

    private void RefreshAttributes(DriveDiscDefinition driveDiscDef, ItemInstanceData instance)
    {
        if (driveDiscMainAttributionText != null)
            driveDiscMainAttributionText.text = driveDiscDef.MainAttributionKey;

        if (driveDiscMainAttributionValueText != null)
        {
            float value = GetMergedValue(driveDiscDef.Attributes, instance.ExtraAttributes, driveDiscDef.MainAttributionKey);
            driveDiscMainAttributionValueText.text = FormatAttributeValue(driveDiscDef.MainAttributionKey, value);
        }

        RefreshMinorAttributes(driveDiscDef, instance);
    }

    private void RefreshMinorAttributes(DriveDiscDefinition driveDiscDef, ItemInstanceData instance)
    {
        if (driveDiscMinorAttributionTexts == null || driveDiscMinorAttributionValueTexts == null)
            return;

        for (int i = 0; i < driveDiscMinorAttributionTexts.Count; i++)
        {
            // 获得key
            string key = i < driveDiscDef.MinorAttributionKey.Count
                ? driveDiscDef.MinorAttributionKey[i]
                : string.Empty;

            // 通过key找value
            if (i < driveDiscMinorAttributionValueTexts.Count && driveDiscMinorAttributionValueTexts[i] != null)
            {
                float value = GetMergedValue(driveDiscDef.Attributes, instance.ExtraAttributes, key);
                // 强化过的次数
                int rollCount = CountRolledKey(instance.RolledMinorAttributeKeys, key);

                // 强化后缀 +1 +2
                string rollSuffix = rollCount > 0 ? $"  +{rollCount}" : string.Empty;

                // 属性名
                if (driveDiscMinorAttributionTexts[i] != null)
                    driveDiscMinorAttributionTexts[i].text = $"{key}{rollSuffix}";

                // 格式化数值
                driveDiscMinorAttributionValueTexts[i].text = $"{FormatAttributeValue(key, value)}";
            }
        }
    }

    private void RefreshMaterialTexts(StrengthenPreviewResult preview)
    {
        int ownedLow = GetOwnedCount(preview, StrengthenMaterialIds.DriveDiscExpLow);
        int ownedMid = GetOwnedCount(preview, StrengthenMaterialIds.DriveDiscExpMid);
        int ownedHigh = GetOwnedCount(preview, StrengthenMaterialIds.DriveDiscExpHigh);

        if (lowCountText != null)
            lowCountText.text = $"{selectedLowCount}/{ownedLow}";

        if (midCountText != null)
            midCountText.text = $"{selectedMidCount}/{ownedMid}";

        if (highCountText != null)
            highCountText.text = $"{selectedHighCount}/{ownedHigh}";
    }

    private void RefreshExpTexts(StrengthenPreviewResult preview)
    {
        //if (addedExpText != null)
        //    addedExpText.text = $"+{preview.AddedExp} EXP";

        if (expText == null)
            return;

        if (preview.PreviewLevel >= StrengthenBalanceConfig.DriveDiscMaxLevel)
        {
            expText.text = "MAX";
            return;
        }

        int needExp = StrengthenBalanceConfig.GetDriveDiscNeedExp(preview.PreviewLevel);
        expText.text = $"{preview.PreviewLevelExp}/{needExp}";
    }

    private void ChangeMaterialCount(int itemId, int delta)
    {
        if (currentItem == null)
            return;

        if (delta > 0 && IsAlreadyPreviewMax())
            return;

        int newLow = selectedLowCount;
        int newMid = selectedMidCount;
        int newHigh = selectedHighCount;

        if (itemId == StrengthenMaterialIds.DriveDiscExpLow)
            newLow = Mathf.Max(0, newLow + delta);

        if (itemId == StrengthenMaterialIds.DriveDiscExpMid)
            newMid = Mathf.Max(0, newMid + delta);

        if (itemId == StrengthenMaterialIds.DriveDiscExpHigh)
            newHigh = Mathf.Max(0, newHigh + delta);

        var driveDiscDef = currentItem.ItemDefinition as DriveDiscDefinition;
        var preview = driveDiscStrengthenService.Preview(saveData, currentItem.ItemInstance, driveDiscDef, newLow, newMid, newHigh);

        if (!preview.Success)
        {
            SetResult(preview.ErrorMessage);
            return;
        }

        selectedLowCount = newLow;
        selectedMidCount = newMid;
        selectedHighCount = newHigh;

        ClearResult();
        RefreshView(false, false, true);
    }

    private void HandleAutoAdd()
    {
        if (currentItem == null)
            return;

        var driveDiscDef = currentItem.ItemDefinition as DriveDiscDefinition;
        var preview = driveDiscStrengthenService.BuildAutoPlan(saveData, currentItem.ItemInstance, driveDiscDef);

        if (!preview.Success)
        {
            SetResult(preview.ErrorMessage);
            return;
        }

        selectedLowCount = GetSelectedCount(preview, StrengthenMaterialIds.DriveDiscExpLow);
        selectedMidCount = GetSelectedCount(preview, StrengthenMaterialIds.DriveDiscExpMid);
        selectedHighCount = GetSelectedCount(preview, StrengthenMaterialIds.DriveDiscExpHigh);

        ClearResult();
        RefreshView(false, true, true);
    }

    private void HandleClear()
    {
        selectedLowCount = 0;
        selectedMidCount = 0;
        selectedHighCount = 0;

        ClearResult();
        RefreshView(true, false, false);
    }

    private void HandleConfirmStrengthen()
    {
        if (currentItem == null || currentItem.ItemInstance == null)
            return;

        var driveDiscDef = currentItem.ItemDefinition as DriveDiscDefinition;
        if (driveDiscDef == null)
            return;

        var result = driveDiscStrengthenService.Apply(
            saveData,
            currentItem.ItemInstance,
            driveDiscDef,
            selectedLowCount,
            selectedMidCount,
            selectedHighCount);

        if (!result.Success)
        {
            SetResult(result.ErrorMessage);
            return;
        }

        // 确认强化后，明确把强化后的实例写回 saveData 中对应槽位/装备位
        WriteBackCurrentItemToSaveData();

        saveGameService.SaveForCurrentAccount(saveData);

        selectedLowCount = 0;
        selectedMidCount = 0;
        selectedHighCount = 0;

        SetResult($"强化成功：Lv.{result.OldLevel} → Lv.{result.NewLevel}");

        if (expSliderAnimator != null)
        {
            expSliderAnimator.PlayPreviewAnimation(
                result.OldLevel,
                result.OldLevelExp,
                result.NewLevel,
                result.NewLevelExp,
                StrengthenBalanceConfig.DriveDiscMaxLevel,
                StrengthenBalanceConfig.GetDriveDiscNeedExp,
                true);
        }

        ResetPreviewSnapshotToCurrent();
        RefreshView(true, false, false);
        onDataChanged?.Invoke();
    }

    // 重置预览快照的方法
    private void ResetPreviewSnapshotToCurrent()
    {
        if (currentItem == null || currentItem.ItemInstance == null)
        {
            lastPreviewLevel = 0;
            lastPreviewExp = 0;
            return;
        }

        lastPreviewLevel = currentItem.ItemInstance.Level;
        lastPreviewExp = currentItem.ItemInstance.CurrentLevelExp;
    }

    // 统一写回当前强化物品到 saveData 的方法
    // 对装备栏强化（SourceSlotIndex = -1）时，按 InstanceId 同步背包里的那一份同物品
    private void WriteBackCurrentItemToSaveData()
    {
        if (saveData == null || currentItem == null || currentItem.ItemInstance == null)
            return;

        ItemInstanceData strengthenedItem = currentItem.ItemInstance;

        if (currentItem.SourceSlotIndex >= 0 &&
            saveData.BackpackData != null &&
            saveData.BackpackData.Slots != null &&
            currentItem.SourceSlotIndex < saveData.BackpackData.Slots.Count &&
            saveData.BackpackData.Slots[currentItem.SourceSlotIndex] != null)
        {
            saveData.BackpackData.Slots[currentItem.SourceSlotIndex].Item = strengthenedItem;
        }
        else
        {
            SyncBackpackItemByInstanceId(strengthenedItem);
        }

        if (currentSlotContext == null || !currentSlotContext.IsDriveDiscSlot || saveData.DriveDiscEquipData == null)
            return;

        switch (currentSlotContext.DriveDiscSlotType)
        {
            case DriveDiscSlotType.One:
                saveData.DriveDiscEquipData.One = strengthenedItem;
                break;
            case DriveDiscSlotType.Two:
                saveData.DriveDiscEquipData.Two = strengthenedItem;
                break;
            case DriveDiscSlotType.Three:
                saveData.DriveDiscEquipData.Three = strengthenedItem;
                break;
            case DriveDiscSlotType.Four:
                saveData.DriveDiscEquipData.Four = strengthenedItem;
                break;
            case DriveDiscSlotType.Five:
                saveData.DriveDiscEquipData.Five = strengthenedItem;
                break;
            case DriveDiscSlotType.Six:
                saveData.DriveDiscEquipData.Six = strengthenedItem;
                break;
        }
    }

    // 按 InstanceId 同步背包物品的方法
    private void SyncBackpackItemByInstanceId(ItemInstanceData strengthenedItem)
    {
        if (strengthenedItem == null ||
            saveData == null ||
            saveData.BackpackData == null ||
            saveData.BackpackData.Slots == null ||
            string.IsNullOrEmpty(strengthenedItem.InstanceId))
            return;

        for (int i = 0; i < saveData.BackpackData.Slots.Count; i++)
        {
            InventorySlotData slot = saveData.BackpackData.Slots[i];
            if (slot == null || slot.Item == null)
                continue;

            if (slot.Item.InstanceId == strengthenedItem.InstanceId)
            {
                slot.Item = strengthenedItem;
                return;
            }
        }
    }

    private bool IsAlreadyPreviewMax()
    {
        if (currentItem == null || currentItem.ItemInstance == null)
            return true;

        var driveDiscDef = currentItem.ItemDefinition as DriveDiscDefinition;
        if (driveDiscDef == null)
            return true;

        var preview = driveDiscStrengthenService.Preview(
            saveData,
            currentItem.ItemInstance,
            driveDiscDef,
            selectedLowCount,
            selectedMidCount,
            selectedHighCount);

        return preview.Success && preview.PreviewLevel >= StrengthenBalanceConfig.DriveDiscMaxLevel;
    }

    private int GetOwnedCount(StrengthenPreviewResult preview, int itemId)
    {
        if (preview == null || preview.MaterialSelections == null)
            return 0;

        for (int i = 0; i < preview.MaterialSelections.Count; i++)
        {
            if (preview.MaterialSelections[i].ItemId == itemId)
                return preview.MaterialSelections[i].OwnedCount;
        }

        return 0;
    }

    private int GetSelectedCount(StrengthenPreviewResult preview, int itemId)
    {
        if (preview == null || preview.MaterialSelections == null)
            return 0;

        for (int i = 0; i < preview.MaterialSelections.Count; i++)
        {
            if (preview.MaterialSelections[i].ItemId == itemId)
                return preview.MaterialSelections[i].SelectedCount;
        }

        return 0;
    }

    private float GetMergedValue(List<ItemAttributeData> baseAttributes, List<ItemAttributeData> extraAttributes, string key)
    {
        return GetValue(baseAttributes, key) + GetValue(extraAttributes, key);
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
            return value.ToString("P0");

        return value.ToString("0.##");
    }

    private void SetResult(string text)
    {
        resultText = text ?? string.Empty;

        ResultPanel.Instance.ShowResult(resultText);
    }

    private void ClearResult()
    {
        resultText = string.Empty;
    }
}