
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
public class CharacterLevelUpPresenter : MonoBehaviour
{
    [Header("Common")]
    [SerializeField] private TextMeshProUGUI characterLevelText;
    [SerializeField] private TextMeshProUGUI previewLevelText;

    [Header("Attributes")]
    [SerializeField] private TextMeshProUGUI characterBaseHP;
    [SerializeField] private TextMeshProUGUI characterBaseAttack;
    [SerializeField] private TextMeshProUGUI characterBaseDefence;

    [Header("Exp")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private StrengthenExpSliderAnimator expSliderAnimator;
    [SerializeField] private TextMeshProUGUI expText;

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
    private string resultText;

    private GameSaveData saveData;
    private SaveGameService saveGameService;
    private CharacterLevelUpService characterLevelUpService;

    private Action onDataChanged;
    private Action onCloseRequested;

    //private EquipmentSlotContext currentSlotContext;
    //private InventoryDisplayItem currentItem;
    private PlayerBaseData playerBaseData;
    private PlayerAttributeData playerAttributeData;

    // 选择材料的数量
    private int selectedLowCount;
    private int selectedMidCount;
    private int selectedHighCount;

    // 记录“上一次点击后的预览等级/经验”，作为下一次动画起点
    private int lastPreviewLevel;
    private int lastPreviewExp;

    public void Initialize(
        GameSaveData saveData,
        SaveGameService saveGameService,
        CharacterLevelUpService characterLevelUpService,
        Action onDataChanged,
        Action onCloseRequested)
    {
        this.saveData = saveData;
        this.saveGameService = saveGameService;
        this.characterLevelUpService = characterLevelUpService;
        this.onDataChanged = onDataChanged;
        this.onCloseRequested = onCloseRequested;

        if (expSliderAnimator != null && expSlider != null)
            expSliderAnimator.SetSlider(expSlider);

        BindButtons();
    }

    public void Bind(PlayerBaseData playerBaseData, PlayerAttributeData playerAttributeData)
    {
        this.playerBaseData = playerBaseData;
        this.playerAttributeData = playerAttributeData;

        selectedLowCount = 0;
        selectedMidCount = 0;
        selectedHighCount = 0;

        // 进入面板时，动画起点重置为真实当前等级/经验
        ResetPreviewSnapshotToCurrent();

        ClearResult();
        RefreshView(true, false, false);
    }

    private void BindButtons()
    {
        if (addLowButton != null)
        {
            addLowButton.onClick.RemoveAllListeners();
            addLowButton.onClick.AddListener(() => ChangeMaterialCount(StrengthenMaterialIds.CharacterExpLow, 1));
        }

        if (reduceLowButton != null)
        {
            reduceLowButton.onClick.RemoveAllListeners();
            reduceLowButton.onClick.AddListener(() => ChangeMaterialCount(StrengthenMaterialIds.CharacterExpLow, -1));
        }

        if (addMidButton != null)
        {
            addMidButton.onClick.RemoveAllListeners();
            addMidButton.onClick.AddListener(() => ChangeMaterialCount(StrengthenMaterialIds.CharacterExpMid, 1));
        }

        if (reduceMidButton != null)
        {
            reduceMidButton.onClick.RemoveAllListeners();
            reduceMidButton.onClick.AddListener(() => ChangeMaterialCount(StrengthenMaterialIds.CharacterExpMid, -1));
        }

        if (addHighButton != null)
        {
            addHighButton.onClick.RemoveAllListeners();
            addHighButton.onClick.AddListener(() => ChangeMaterialCount(StrengthenMaterialIds.CharacterExpHigh, 1));
        }

        if (reduceHighButton != null)
        {
            reduceHighButton.onClick.RemoveAllListeners();
            reduceHighButton.onClick.AddListener(() => ChangeMaterialCount(StrengthenMaterialIds.CharacterExpHigh, -1));
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

    // 是否点击自动添加按钮 是否更新上一次预览状态
    private void RefreshView(bool instantSlider, bool useVisualLevelUpLimit, bool updateSnapshot)
    {
        if (playerBaseData == null || playerAttributeData == null)
            return;

        var preview = characterLevelUpService.Preview(
            saveData,
            playerBaseData,
            playerAttributeData,
            selectedLowCount,
            selectedMidCount,
            selectedHighCount);

        if (!preview.Success)
        {
            SetResult(preview.ErrorMessage);
            return;
        }

        if (characterLevelText != null)
            characterLevelText.text = $"{playerBaseData.Level}";

        if (previewLevelText != null)
            previewLevelText.text = preview.PreviewLevel == playerBaseData.Level
                ? $"{playerBaseData.Level}"
                : $"{preview.PreviewLevel}";

        RefreshAttributes(playerBaseData, playerAttributeData);
        RefreshMaterialTexts(preview);
        RefreshExpTexts(preview);

        if (expSliderAnimator != null)
        {
            if (instantSlider)
            {
                // 设置经验条值
                expSliderAnimator.SetInstant(
                    playerBaseData.Level,
                    playerBaseData.CurrentLevelExp,
                    StrengthenBalanceConfig.CharacterMaxLevel,
                    StrengthenBalanceConfig.GetCharacterNeedExp);
            }
            else
            {
                // 播放经验动画
                // 动画起点改为“上一次预览状态”，不是每次都从真实当前值开始
                expSliderAnimator.PlayPreviewAnimation(
                    lastPreviewLevel,
                    lastPreviewExp,
                    preview.PreviewLevel,
                    preview.PreviewLevelExp,
                    StrengthenBalanceConfig.CharacterMaxLevel,
                    StrengthenBalanceConfig.GetCharacterNeedExp,
                    useVisualLevelUpLimit);
            }
        }

        // ===== 修改：只有手动/自动预览变化时才更新快照；
        // ===== instant clear / 初始绑定 不在这里改快照 =====
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

    // 刷新属性
    private void RefreshAttributes(PlayerBaseData playerBaseData, PlayerAttributeData playerAttributeData)
    {
        if (playerAttributeData == null)
            return;

        if (characterBaseHP != null)
            characterBaseHP.text = playerAttributeData.BaseHP.ToString();

        if (characterBaseAttack != null)
            characterBaseAttack.text = playerAttributeData.BaseAttack.ToString();

        if (characterBaseDefence != null)
            characterBaseDefence.text = playerAttributeData.BaseDefense.ToString();
    }

    // 刷新材料Text
    private void RefreshMaterialTexts(StrengthenPreviewResult preview)
    {
        int ownedLow = GetOwnedCount(preview, StrengthenMaterialIds.CharacterExpLow);
        int ownedMid = GetOwnedCount(preview, StrengthenMaterialIds.CharacterExpMid);
        int ownedHigh = GetOwnedCount(preview, StrengthenMaterialIds.CharacterExpHigh);

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
        if (expText == null)
            return;

        if (preview.PreviewLevel >= StrengthenBalanceConfig.CharacterMaxLevel)
        {
            expText.text = "MAX";
            return;
        }

        // 预览等级那一阶段多出来的经验值 / 预览等级所需要的经验值
        int needExp = StrengthenBalanceConfig.GetCharacterNeedExp(preview.PreviewLevel);
        expText.text = $"{preview.PreviewLevelExp}/{needExp}";
    }

    // 更改材料数量
    private void ChangeMaterialCount(int itemId, int delta)
    {
        // 只有继续增加时才阻止“已达预览满级”；
        // reduce 仍然允许
        if (delta > 0 && IsAlreadyPreviewMax())
            return;

        // 已选中的材料数量
        int newLow = selectedLowCount;
        int newMid = selectedMidCount;
        int newHigh = selectedHighCount;

        if (itemId == StrengthenMaterialIds.CharacterExpLow)
            newLow = Mathf.Max(0, newLow + delta);

        if (itemId == StrengthenMaterialIds.CharacterExpMid)
            newMid = Mathf.Max(0, newMid + delta);

        if (itemId == StrengthenMaterialIds.CharacterExpHigh)
            newHigh = Mathf.Max(0, newHigh + delta);

        var preview = characterLevelUpService.Preview(
            saveData,
            playerBaseData,
            playerAttributeData,
            newLow,
            newMid,
            newHigh);

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
        // 手动点材料按钮，不使用 maxVisualLevelUpCount 限制
        RefreshView(false, false, true);
    }

    // 自动添加
    private void HandleAutoAdd()
    {
        var preview = characterLevelUpService.BuildAutoPlan(
            saveData,
            playerBaseData,
            playerAttributeData);

        if (!preview.Success)
        {
            SetResult(preview.ErrorMessage);
            return;
        }

        selectedLowCount = GetSelectedCount(preview, StrengthenMaterialIds.CharacterExpLow);
        selectedMidCount = GetSelectedCount(preview, StrengthenMaterialIds.CharacterExpMid);
        selectedHighCount = GetSelectedCount(preview, StrengthenMaterialIds.CharacterExpHigh);

        ClearResult();
        // 播放经验条动画
        // 只有自动添加时，才使用 maxVisualLevelUpCount 限制
        RefreshView(false, true, true);
    }

    // 清除按钮
    private void HandleClear()
    {
        selectedLowCount = 0;
        selectedMidCount = 0;
        selectedHighCount = 0;

        ClearResult();

        // clear 后直接回到真实当前经验，并重置快照
        ResetPreviewSnapshotToCurrent();
        RefreshView(true, false, false);
    }

    // 确认强化
    private void HandleConfirmStrengthen()
    {
        if (playerBaseData == null || playerAttributeData == null)
            return;

        var result = characterLevelUpService.Apply(
            saveData,
            playerBaseData,
            playerAttributeData,
            selectedLowCount,
            selectedMidCount,
            selectedHighCount);

        if (!result.Success)
        {
            SetResult(result.ErrorMessage);
            return;
        }

        // 确认升级后，明确把角色等级与属性写回 saveData 主对象
        saveData.PlayerBaseData = playerBaseData;
        saveData.PlayerAttributeData = playerAttributeData;

        saveGameService.SaveForCurrentAccount(saveData);

        selectedLowCount = 0;
        selectedMidCount = 0;
        selectedHighCount = 0;

        string template = LuaDemoBridge.Instance.GetText("StrengthenSuccess");
        SetResult(string.Format(template, result.OldLevel, result.NewLevel));

        // 确认后以真实新值为准，重置快照并直接落位
        ResetPreviewSnapshotToCurrent();
        RefreshView(true, false, false);
        onDataChanged?.Invoke();
    }

    // 重置预览快照的方法
    private void ResetPreviewSnapshotToCurrent()
    {
        if (playerBaseData == null)
        {
            lastPreviewLevel = 0;
            lastPreviewExp = 0;
            return;
        }

        lastPreviewLevel = playerBaseData.Level;
        lastPreviewExp = playerBaseData.CurrentLevelExp;
    }

    // 预览等级是否已经满级
    private bool IsAlreadyPreviewMax()
    {
        var preview = characterLevelUpService.Preview(
            saveData,
            playerBaseData,
            playerAttributeData,
            selectedLowCount,
            selectedMidCount,
            selectedHighCount);

        return preview.Success && preview.PreviewLevel >= StrengthenBalanceConfig.CharacterMaxLevel;
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