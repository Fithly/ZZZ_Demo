using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessagePanel : BasePanel
{
    public TextMeshProUGUI text;
    string message = null;

    public override void OnEnter()
    {
        base.OnEnter();

        // 过渡Alpha值    Alpha值 时间 隐藏
        text.CrossFadeAlpha(0, 0.2f, false);
        uiManager.SetMessagePanel(this);
    }

    private void Update()
    {
        if (message != null)
        {
            // 需要异步显示
            ShowText(message);
            message = null;
        }
    }

    // 显示内容            参数：内容， 是否是异步
    public void ShowMessage(string str, bool sync = false)
    {
        if (sync)
        {
            // 异步显示
            message = str;
        }
        else
        {
            // 直接显示
            ShowText(str);
        }

    }

    private void ShowText(string str)
    {
        text.text = str;
        // 变成不透明（立马显示）
        text.CrossFadeAlpha(1, 0.1f, false);
        // 延时调用
        Invoke("HideText", 1);
    }

    private void HideText()
    {
        text.CrossFadeAlpha(0, 0.2f, false);
    }
}
