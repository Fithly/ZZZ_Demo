using UnityEngine;

/// <summary>
/// 所有 UI 面板的统一基类
/// 统一 Show / Hide
/// </summary>
public class UIPanelBase : MonoBehaviour
{
    [SerializeField] public GameObject root;

    public virtual void Show()
    {
        if (root != null)
            root.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public virtual void Hide()
    {
        if (root != null)
            root.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public virtual void SetVisible(bool visible)
    {
        if (visible) Show();
        else Hide();
    }
}