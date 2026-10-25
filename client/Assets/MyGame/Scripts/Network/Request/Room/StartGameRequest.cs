using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketGameProtocol;

public class StartGameRequest : BaseRequest
{
    private MainPack pack = null;

    // OnResponse 来自 Socket 异步线程，Update 在主线程读，增加锁
    private readonly object packLock = new object();

    public RoomPanel roomPanel;

    public override void Awake()
    {
        requestCode = RequestCode.Room;
        actionCode = ActionCode.StartGame;
        base.Awake();
    }

    private void Update()
    {
        MainPack responsePack = null;

        lock (packLock)
        {
            if (pack != null)
            {
                responsePack = pack;
                pack = null;
            }
        }

        if (responsePack == null)
            return;

        roomPanel.StartGameResponse(responsePack);

        // 修改：
        // 这里不直接切 BattleScene。
        // 房主和成员都统一等待服务端广播 Starting，
        // StartingRequest 收到后再调用 RoomStartGameBridge.HandleStarting()。
    }

    public void SendRequest()
    {
        MainPack pack = new MainPack();
        pack.Requestcode = requestCode;
        pack.Actioncode = actionCode;
        pack.Str = "r";

        base.SendRequest(pack);
    }

    public override void OnResponse(MainPack pack)
    {
        lock (packLock)
        {
            this.pack = pack;
        }
    }
}
