using SocketGameProtocol;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginPanel : BasePanel
{
    [SerializeField] private LoginRequest loginRequest;
    [SerializeField] private TMP_InputField user, passWord;
    [SerializeField] private Button loginButton, switchButton;

    private void Start()
    {
        loginButton.onClick.AddListener(OnLoginClick);
        switchButton.onClick.AddListener(SwitchLogon);
    }


    private void OnLoginClick()
    {
        if (user.text == "" || passWord.text == "")
        {
            Debug.LogWarning("用户名密码不能为空");
            return;
        }
        loginRequest.SendRequest(user.text, passWord.text);
    }

    private void SwitchLogon()
    {
        uiManager.PushPanel(PanelType.Logon);
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
                uiManager.ShowMessage("登录成功！");

                // 登录成功后记录本地玩家名
                // 后续从房间切到战斗场景时，用它判断谁是本地玩家
                if (GameFace.Instance != null)
                {
                    GameFace.Instance.SetSelfID(user.text);
                }

                // 登录成功后记录当前账号，后续 UI 存档会根据这个账号读写专属 Json。
                CurrentAccountSession.SetLoginUser(user.text, user.text);

                uiManager.PushPanel(PanelType.RoomList);
                break;
            case ReturnCode.Fail:
                uiManager.ShowMessage("登录失败！");
                break;
            default:
                Debug.Log("def");
                break;
        }
    }

    private void OnDestroy()
    {
        loginButton.onClick.RemoveListener(OnLoginClick);
        switchButton.onClick.RemoveListener(SwitchLogon);
    }
}
