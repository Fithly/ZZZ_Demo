using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultPanel : Singleton<ResultPanel>
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button closeBtn;

    protected override void Awake()
    {
        base.Awake();                  // 必须调用基类 Awake 以保证单例逻辑
        //DontDestroyOnLoad(gameObject); // 保持跨场景存在

        // 初始化时默认隐藏
        if (panel != null)
            panel.SetActive(false);

        if (closeBtn != null)
        {
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(() => HideResult());
        }
    }

    public void ShowResult(string text)
    {
        if (panel == null)
            return;

        panel.SetActive(true);
        if(resultText != null) 
            resultText.text = text;
    }

    private void HideResult()
    {
        if (panel == null)
            return;
        panel.SetActive(false);

        // 关闭时顺便清空显示文本
        if (resultText != null)
            resultText.text = string.Empty;
    }
}
