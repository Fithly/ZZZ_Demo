using SocketGameProtocol;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogonPanel : BasePanel
{
    [SerializeField] private LogonRequest logonRequest;
    [SerializeField] private TMP_InputField user, passWord;
    [SerializeField] private Button logonButton, switchButton;

    private void Start()
    {
        logonButton.onClick.AddListener(OnLogonClick);
        switchButton.onClick.AddListener(SwitchLogin);
    }

    private void SwitchLogin()
    {
        uiManager.PopPanel();
    }

    private void OnLogonClick()
    {
        if (user.text == "" || passWord.text == "")
        {
            Debug.LogWarning("用户名密码不能为空");
            return;
        }
        logonRequest.SendRequest(user.text, passWord.text);
    }

    public override void OnEnter()
    {
        base.OnEnter();
        Enter();
    }

    public override void OnExit()
    {
        base.OnExit();
        Exit();
    }

    public override void OnPause()
    {
        base.OnPause();
        Exit();
    }

    public override void OnRecovery()
    {
        base.OnRecovery();
        Enter();
    }

    private void Enter()
    {
        gameObject.SetActive(true);
    }

    private void Exit()
    {
        gameObject.SetActive(false);
    }

    public void OnResponse(MainPack pack)
    {
        switch (pack.Returncode)
        {
            case ReturnCode.Succeed:
                uiManager.ShowMessage("注册成功！");
                uiManager.PushPanel(PanelType.Login);
                break;
            case ReturnCode.Fail:
                uiManager.ShowMessage("注册失败！");
                break;
            default:
                Debug.Log("def");
                break;
        }
    }

    private void OnDestroy()
    {
        logonButton.onClick.RemoveListener(OnLogonClick);
        switchButton.onClick.RemoveListener(SwitchLogin);
    }
}
