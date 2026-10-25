using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 武器强化面板显示器
/// 
/// 沿用你现在 EquipCandidateDetailPresenter 已有的字段语义
/// 
/// 读实例等级 + 读 DefinitionKey + 读实例强化值
/// </summary>
public class WeaponStrengthenPresenter : MonoBehaviour
{
    [Header("Common")]
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI weaponLevelText;
    [SerializeField] private TextMeshProUGUI previewLevelText;
    [SerializeField] private List<GameObject> weaponStarObjects;

    private IItemIconProvider iconProvider;
    [SerializeField] private Image weaponIcon;

    [Header("Attributes")]
    [SerializeField] private TextMeshProUGUI weaponBaseAttributionText;
    [SerializeField] private TextMeshProUGUI weaponBaseAttributionValueText;
    [SerializeField] private TextMeshProUGUI weaponAdvanceAttributionText;
    [SerializeField] private TextMeshProUGUI weaponAdvanceAttributionValueText;

    [Header("Exp")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private StrengthenExpSliderAnimator expSliderAnimator;
    [SerializeField] private TextMeshProUGUI expText;
    //[SerializeField] private TextMeshProUGUI addedExpText;

    [Header("Low Material")]
    [SerializeField] private Button addLowButton;                // 添加
    [SerializeField] private Button reduceLowButton;             // 减少
    [SerializeField] private TextMeshProUGUI lowCountText;       // 数量

    [Header("Mid Material")]
    [SerializeField] private Button addMidButton;
    [SerializeField] private Button reduceMidButton;
    [SerializeField] private TextMeshProUGUI midCountText;

    [Header("High Material")]
    [SerializeField] private Button addHighButton;
    [SerializeField] private Button reduceHighButton;
    [SerializeField] private TextMeshProUGUI highCountText;

    [Header("Buttons")]
    [SerializeField] private Button autoAddButton;               // 自动添加
    [SerializeField] private Button clearButton;                 // 全部清除
    [SerializeField] private Button closeButton;                 // 关闭面板
    [SerializeField] private Button confirmStrengthenButton;     // 确认升级

    [Header("Result")]
    private string resultText = string.Empty;

    private GameSaveData saveData;
    private SaveGameService saveGameService;
    private WeaponStrengthenService weaponStrengthenService;

    private Action onDataChanged;
    private Action onCloseRequested;

    private EquipmentSlotContext currentSlotContext;
    private InventoryDisplayItem currentItem;

    // 选择材料的数量
    private int selectedLowCount;
    private int selectedMidCount;
    private int selectedHighCount;

    // 记录“上一次点击后的预览等级/经验”
    private int lastPreviewLevel;
    private int lastPreviewExp;

    public void Initialize(
        GameSaveData saveData,
        SaveGameService saveGameService,
        WeaponStrengthenService weaponStrengthenService,
        IItemIconProvider iconProvider,
        Action onDataChanged,
        Action onCloseRequested)
    {
        this.saveData = saveData;
        this.saveGameService = saveGameService;
        this.weaponStrengthenService = weaponStrengthenService;
        this.iconProvider = iconProvider;
        this.onDataChanged = onDataChanged;
        this.onCloseRequested = onCloseRequested;

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

        ResetPreviewSnapshotToCurrent();

        ClearResult();
        RefreshView(true, false, false);
    }

    private void BindButtons()
    {
        if (addLowButton != null)
        {
            addLowButton.onClick.RemoveAllListeners();
            addLowButton.onClick.AddListener(() => ChangeMaterialCount(StrengthenMaterialIds.WeaponExpLow, 1));
        }

        if (reduceLowButton != null)
        {
            reduceLowButton.onClick.RemoveAllListeners();
            reduceLowButton.onClick.AddListener(() => ChangeMaterialCount(StrengthenMaterialIds.WeaponExpLow, -1));
        }

        if (addMidButton != null)
        {
            addMidButton.onClick.RemoveAllListeners();
            addMidButton.onClick.AddListener(() => ChangeMaterialCount(StrengthenMaterialIds.WeaponExpMid, 1));
        }

        if (reduceMidButton != null)
        {
            reduceMidButton.onClick.RemoveAllListeners();
            reduceMidButton.onClick.AddListener(() => ChangeMaterialCount(StrengthenMaterialIds.WeaponExpMid, -1));
        }

        if (addHighButton != null)
        {
            addHighButton.onClick.RemoveAllListeners();
            addHighButton.onClick.AddListener(() => ChangeMaterialCount(StrengthenMaterialIds.WeaponExpHigh, 1));
        }

        if (reduceHighButton != null)
        {
            reduceHighButton.onClick.RemoveAllListeners();
            reduceHighButton.onClick.AddListener(() => ChangeMaterialCount(StrengthenMaterialIds.WeaponExpHigh, -1));
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

        var weaponDef = currentItem.ItemDefinition as WeaponDefinition;
        var instance = currentItem.ItemInstance;

        if (weaponDef == null)
            return;

        var preview = weaponStrengthenService.Preview(
            saveData,
            instance,
            weaponDef,
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
            icon = iconProvider.GetIcon(weaponDef);

        if (icon == null) 
            Debug.Log("Icon is Empty");

        if (weaponIcon != null)
            weaponIcon.sprite = icon;

        if (weaponNameText != null)
            weaponNameText.text = weaponDef.Name;

        if (weaponLevelText != null)
            weaponLevelText.text = $"{instance.Level}";

        if (previewLevelText != null)
            previewLevelText.text = preview.PreviewLevel == instance.Level
                ? $"{instance.Level}"
                : $"{preview.PreviewLevel}";

        // 星级
        if (weaponStarObjects != null)
        {
            for (int i = 0; i < weaponStarObjects.Count; i++)
            {
                if (weaponStarObjects[i] != null)
                    weaponStarObjects[i].SetActive(i < weaponDef.StartNumber);
            }
        }

        RefreshAttributes(weaponDef, instance);
        RefreshMaterialTexts(preview);
        RefreshExpTexts(preview);

        if (expSliderAnimator != null)
        {
            if (instantSlider)
            {
                // 设置经验条值
                expSliderAnimator.SetInstant(
                    instance.Level,
                    instance.CurrentLevelExp,
                    StrengthenBalanceConfig.WeaponMaxLevel,
                    StrengthenBalanceConfig.GetWeaponNeedExp);
            }
            else
            {
                // 播放经验动画
                expSliderAnimator.PlayPreviewAnimation(
                    preview.CurrentLevel,
                    preview.CurrentLevelExp,
                    preview.PreviewLevel,
                    preview.PreviewLevelExp,
                    StrengthenBalanceConfig.WeaponMaxLevel,
                    StrengthenBalanceConfig.GetWeaponNeedExp);
            }
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

    // 刷新属性
    private void RefreshAttributes(WeaponDefinition weaponDef, ItemInstanceData instance)
    {
        if (weaponBaseAttributionText != null)
            weaponBaseAttributionText.text = weaponDef.BaseAttributionKey;

        if (weaponAdvanceAttributionText != null)
            weaponAdvanceAttributionText.text = weaponDef.AdvanceAttributionKey;

        // 基础属性
        if (weaponBaseAttributionValueText != null)
        {
            // 得到定义值+实例增长值  再格式化输出
            float value = GetMergedValue(weaponDef.Attributes, instance.ExtraAttributes, weaponDef.BaseAttributionKey);
            weaponBaseAttributionValueText.text = FormatAttributeValue(weaponDef.BaseAttributionKey, value);
        }

        // 强化属性
        if (weaponAdvanceAttributionValueText != null)
        {
            float value = GetMergedValue(weaponDef.Attributes, instance.ExtraAttributes, weaponDef.AdvanceAttributionKey);
            weaponAdvanceAttributionValueText.text = FormatAttributeValue(weaponDef.AdvanceAttributionKey, value);
        }
    }

    // 刷新材料Text
    private void RefreshMaterialTexts(StrengthenPreviewResult preview)
    {
        int ownedLow = GetOwnedCount(preview, StrengthenMaterialIds.WeaponExpLow);
        int ownedMid = GetOwnedCount(preview, StrengthenMaterialIds.WeaponExpMid);
        int ownedHigh = GetOwnedCount(preview, StrengthenMaterialIds.WeaponExpHigh);

        if (lowCountText != null)
            lowCountText.text = $"{selectedLowCount}/{ownedLow}";

        if (midCountText != null)
            midCountText.text = $"{selectedMidCount}/{ownedMid}";

        if (highCountText != null)
            highCountText.text = $"{selectedHighCount}/{ownedHigh}";
    }

    // 刷新经验值Text
    private void RefreshExpTexts(StrengthenPreviewResult preview)
    {
        //if (addedExpText != null)
        //    addedExpText.text = $"+{preview.AddedExp} EXP";

        if (expText == null)
            return;

        if (preview.PreviewLevel >= StrengthenBalanceConfig.WeaponMaxLevel)
        {
            expText.text = "MAX";
            return;
        }

        // 预览等级那一阶段多出来的经验值 / 预览等级所需要的经验值
        int needExp = StrengthenBalanceConfig.GetWeaponNeedExp(preview.PreviewLevel);
        expText.text = $"{preview.PreviewLevelExp}/{needExp}";
    }

    // 更改材料数量
    private void ChangeMaterialCount(int itemId, int delta)
    {
        if (currentItem == null)
            return;

        if (delta > 0 && IsAlreadyPreviewMax())
            return;

        // 已选中的材料数量
        int newLow = selectedLowCount;
        int newMid = selectedMidCount;
        int newHigh = selectedHighCount;

        if (itemId == StrengthenMaterialIds.WeaponExpLow)
            newLow = Mathf.Max(0, newLow + delta);

        if (itemId == StrengthenMaterialIds.WeaponExpMid)
            newMid = Mathf.Max(0, newMid + delta);

        if (itemId == StrengthenMaterialIds.WeaponExpHigh)
            newHigh = Mathf.Max(0, newHigh + delta);

        var weaponDef = currentItem.ItemDefinition as WeaponDefinition;
        var preview = weaponStrengthenService.Preview(saveData, currentItem.ItemInstance, weaponDef, newLow, newMid, newHigh);

        // 告知结果出错
        if (!preview.Success)
        {
            SetResult(preview.ErrorMessage);
            return;
        }

        selectedLowCount = newLow;
        selectedMidCount = newMid;
        selectedHighCount = newHigh;

        ClearResult();
        // 播放经验条动画
        RefreshView(false, false, true);
    }

    // 自动添加
    private void HandleAutoAdd()
    {
        if (currentItem == null)
            return;

        var weaponDef = currentItem.ItemDefinition as WeaponDefinition;
        var preview = weaponStrengthenService.BuildAutoPlan(saveData, currentItem.ItemInstance, weaponDef);

        if (!preview.Success)
        {
            SetResult(preview.ErrorMessage);
            return;
        }

        selectedLowCount = GetSelectedCount(preview, StrengthenMaterialIds.WeaponExpLow);
        selectedMidCount = GetSelectedCount(preview, StrengthenMaterialIds.WeaponExpMid);
        selectedHighCount = GetSelectedCount(preview, StrengthenMaterialIds.WeaponExpHigh);

        ClearResult();
        // 播放经验条动画
        RefreshView(false, true, true);
    }

    // 清除按钮
    private void HandleClear()
    {
        selectedLowCount = 0;
        selectedMidCount = 0;
        selectedHighCount = 0;

        ClearResult();
        // 直接设置经验条数值
        RefreshView(true, false, false);
    }

    // 确认强化
    private void HandleConfirmStrengthen()
    {
        if (currentItem == null || currentItem.ItemInstance == null)
            return;

        var weaponDef = currentItem.ItemDefinition as WeaponDefinition;
        if (weaponDef == null)
            return;

        var result = weaponStrengthenService.Apply(
            saveData,
            currentItem.ItemInstance,
            weaponDef,
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
            // 直接播放强化动画
            expSliderAnimator.PlayPreviewAnimation(
                result.OldLevel,
                result.OldLevelExp,
                result.NewLevel,
                result.NewLevelExp,
                StrengthenBalanceConfig.WeaponMaxLevel,
                StrengthenBalanceConfig.GetWeaponNeedExp,
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

        if (currentSlotContext != null && currentSlotContext.IsWeaponSlot)
            saveData.EquippedWeapon = strengthenedItem;
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

    // 预览等级是否已经满级
    private bool IsAlreadyPreviewMax()
    {
        if (currentItem == null || currentItem.ItemInstance == null)
            return true;

        var weaponDef = currentItem.ItemDefinition as WeaponDefinition;
        if (weaponDef == null)
            return true;

        var preview = weaponStrengthenService.Preview(
            saveData,
            currentItem.ItemInstance,
            weaponDef,
            selectedLowCount,
            selectedMidCount,
            selectedHighCount);

        return preview.Success && preview.PreviewLevel >= StrengthenBalanceConfig.WeaponMaxLevel;
    }

    // 获得物品的总数目
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

    // 格式化数值
    private string FormatAttributeValue(string key, float value)
    {
        // 是百分比属性，用百分比格式保留两位小数，例如 0.1234 -> "12.34%"
        if (key == AttributeKeyUtility.CritRate || key == AttributeKeyUtility.CritDamage)
            return $"{value:P0}";

        // 普通属性，直接保留最多两位小数显示，例如 12.5 -> "12.5"
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