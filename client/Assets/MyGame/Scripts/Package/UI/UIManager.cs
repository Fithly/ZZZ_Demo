using UnityEngine;

/// <summary>
/// 管理背包界面和角色界面的开关
/// 保证当前主界面只有一个显示
/// </summary>
public class UIManager : Singleton<UIManager>
{
    [SerializeField] private BackpackPanelController backpackPanel;
    [SerializeField] private CharacterPanelController characterPanel;

    private UIPageType currentPage = UIPageType.None;

    private void Start()
    {
        if (backpackPanel != null && characterPanel != null)
            CloseAllPages();

        RefreshCursorState();
    }

    public void OpenPage(UIPageType pageType)
    {
        currentPage = pageType;
        RefreshPages();
        RefreshCursorState();
    }

    public void CloseAllPages()
    {
        currentPage = UIPageType.None;
        RefreshPages();
        RefreshCursorState();
    }

    public void OpenBackpack()
    {
        OpenPage(UIPageType.Backpack);
    }

    public void OpenCharacter()
    {
        OpenPage(UIPageType.Character);
        characterPanel.ShowBaseTab();
    }

    private void RefreshPages()
    {
        if (backpackPanel != null)
            backpackPanel.SetVisible(currentPage == UIPageType.Backpack);

        if (characterPanel != null)
            characterPanel.SetVisible(currentPage == UIPageType.Character);
    }

    // 判断是否有主界面打开
    public bool IsAnyMainPanelOpen()
    {
        bool backpackOpen = backpackPanel != null && backpackPanel.root.gameObject.activeSelf;
        bool charactorOpen = characterPanel != null && characterPanel.root.gameObject.activeSelf;

        return backpackOpen || charactorOpen;
    }

    // 统一刷新鼠标状态
    private void RefreshCursorState()
    {
        if (IsAnyMainPanelOpen())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}