using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterBasePanelPresenter : MonoBehaviour
{
    //[SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI levelText;

    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI critRateText;
    [SerializeField] private TextMeshProUGUI critDamageText;

    // ===== 修改：新增角色升级按钮 =====
    [SerializeField] private Button levelUpButton;

    private Action onLevelUpClicked;

    // ===== 修改：新增初始化方法，用于注册按钮事件 =====
    public void Initialize(Action onLevelUpClicked)
    {
        this.onLevelUpClicked = onLevelUpClicked;

        if (levelUpButton != null)
        {
            levelUpButton.onClick.RemoveAllListeners();
            levelUpButton.onClick.AddListener(HandleLevelUpClicked);
        }
    }

    public void Refresh(GameSaveData saveData, CharacterAttributeService attributeService)
    {
        if (saveData == null || attributeService == null)
            return;

        //if (playerNameText != null)
        //    playerNameText.text = saveData.PlayerBaseData.PlayerName;

        if (levelText != null)
            levelText.text = $"{saveData.PlayerBaseData.Level}";

        var finalAttributes = attributeService.BuildFinalAttributes(saveData);

        if (hpText != null)
            hpText.text = $"{finalAttributes.HP}";

        if (attackText != null)
            attackText.text = $"{finalAttributes.Attack}";

        if (defenseText != null)
            defenseText.text = $"{finalAttributes.Defense}";

        if (critRateText != null)
            critRateText.text = $"{finalAttributes.CritRate:P0}";

        if (critDamageText != null)
            critDamageText.text = $"{finalAttributes.CritDamage:P0}";
    }

    private void HandleLevelUpClicked()
    {
        onLevelUpClicked?.Invoke();
    }
}