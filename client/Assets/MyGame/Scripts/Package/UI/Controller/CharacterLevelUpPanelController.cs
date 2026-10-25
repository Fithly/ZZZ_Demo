using System;
using System.Xml.Xsl;
using UnityEngine;

/// <summary>
/// 角色等级升级面板控制器
/// </summary>
public class CharacterLevelUpPanelController : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private CharacterLevelUpPresenter characterLevelUpPresenter;

    private GameSaveData saveData;
    private SaveGameService saveGameService;
    private CharacterLevelUpService characterLevelUpService;

    private Action onDataChanged;

    private PlayerBaseData playerBaseData;
    private PlayerAttributeData playerAttributeData;

    public void Initialize(
        GameSaveData saveData,
        SaveGameService saveGameService,
        PlayerBaseData playerBaseData,
        PlayerAttributeData playerAttributeData,
        CharacterLevelUpService characterLevelUpService,
        Action onDataChanged)
    {
        this.saveData = saveData;
        this.saveGameService = saveGameService;
        this.playerBaseData = playerBaseData;
        this.playerAttributeData = playerAttributeData;
        this.characterLevelUpService = characterLevelUpService;
        this.onDataChanged = onDataChanged;

        if (characterLevelUpPresenter != null)
        {
            characterLevelUpPresenter.Initialize(
                saveData,
                saveGameService,
                characterLevelUpService,
                HandleStrengthenDataChanged,
                Close);
        }

        HideAll();
    }

    public void Open(PlayerBaseData playerData, PlayerAttributeData playerAttrData)
    {
        playerBaseData = playerData;
        playerAttributeData = playerAttrData;

        HideAll();

        if (root != null)
            root.SetActive(true);

        if (playerBaseData == null || playerAttributeData == null)
            return;

        if (characterLevelUpPresenter != null)
        {
            characterLevelUpPresenter.gameObject.SetActive(true);
            characterLevelUpPresenter.Bind(playerBaseData, playerAttributeData);
        }
    }

    public void Close()
    {
        HideAll();
    }

    private void HandleStrengthenDataChanged()
    {
        onDataChanged?.Invoke();
    }

    private void HideAll()
    {
        if (root != null)
            root.SetActive(false);

        if (characterLevelUpPresenter != null)
            characterLevelUpPresenter.gameObject.SetActive(false);
    }
}