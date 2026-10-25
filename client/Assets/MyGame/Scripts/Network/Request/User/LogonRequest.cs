using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketGameProtocol;

public class LogonRequest : BaseRequest
{
    public LogonPanel logonPanel;
    private MainPack pack = null;

    public override void Awake()
    {
        requestCode = RequestCode.User;
        actionCode = ActionCode.Logon;
        base.Awake();
    }

    private void Update()
    {
        // 将最开始Client的回调异步 转变为主线程调用
        // Unity的组件只能在主线程访问（调用UI）
        if (pack != null)
        {
            // 调用注册面板去判断 并调整UI
            logonPanel.OnResponse(pack);
            pack = null;
        }
    }

    public override void OnResponse(MainPack pack)
    {
        // 赋值 在Update中启动处理
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
