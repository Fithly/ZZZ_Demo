using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketGameProtocol;

public class LoginRequest : BaseRequest
{
    public LoginPanel loginPanel;
    private MainPack pack = null;

    public override void Awake()
    {
        requestCode = RequestCode.User;
        actionCode = ActionCode.Login;
        base.Awake();
    }

    private void Update()
    {
        if (pack != null)
        {
            loginPanel.OnResponse(pack);
            pack = null;
        }
    }

    public override void OnResponse(MainPack pack)
    {
        this.pack = pack;
    }

    public void SendRequest(string user, string pass)
    {
        MainPack pack = new MainPack();
        pack.Requestcode = requestCode;
        pack.Actioncode = actionCode;

        // 登录和注册用的是同一个proto类型
        LoginPack loginPack = new LoginPack();
        loginPack.Username = user;
        loginPack.Password = pass;
        pack.Loginpack = loginPack;

        base.SendRequest(pack);
    }
}
