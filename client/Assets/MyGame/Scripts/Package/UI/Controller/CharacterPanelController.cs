using UnityEngine;

/// <summary>
/// 管角色界面三页签切换
/// 
/// 管按钮高亮 object
/// 
/// 基础面板/技能面板/装备面板显示隐藏
/// </summary>
public class CharacterPanelController : UIPanelBase
{
    // 菜单高亮
    [Header("Tab Selected Objects")]
    [SerializeField] private GameObject baseTabSelectedObject;
    [SerializeField] private GameObject skillTabSelectedObject;
    [SerializeField] private GameObject equipTabSelectedObject;

    // 不同面板
    [Header("Panels")]
    [SerializeField] private GameObject basePanel;
    [SerializeField] private GameObject skillPanel;
    [SerializeField] private GameObject equipPanel;

    private CharacterTabType currentTab = CharacterTabType.Base;

    public void ShowBaseTab()
    {
        currentTab = CharacterTabType.Base;
        RefreshTabs();
    }

    public void ShowSkillTab()
    {
        currentTab = CharacterTabType.Skill;
        RefreshTabs();
    }

    public void ShowEquipTab()
    {
        currentTab = CharacterTabType.Equip;
        RefreshTabs();
    }

    private void Awake()
    {
        RefreshTabs();
    }

    private void RefreshTabs()
    {
        if (baseTabSelectedObject != null)
            baseTabSelectedObject.SetActive(currentTab == CharacterTabType.Base);

        if (skillTabSelectedObject != null)
            skillTabSelectedObject.SetActive(currentTab == CharacterTabType.Skill);

        if (equipTabSelectedObject != null)
            equipTabSelectedObject.SetActive(currentTab == CharacterTabType.Equip);

        if (basePanel != null)
            basePanel.SetActive(currentTab == CharacterTabType.Base);

        if (skillPanel != null)
            skillPanel.SetActive(currentTab == CharacterTabType.Skill);

        if (equipPanel != null)
            equipPanel.SetActive(currentTab == CharacterTabType.Equip);
    }
}