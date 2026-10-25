using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketGameProtocol;

public class ExitRoomRequest : BaseRequest
{
    private bool isExit  = false;

    public RoomPanel roomPanel;

    public override void Awake()
    {
        requestCode = RequestCode.Room;
        actionCode = ActionCode.Exit;
        base.Awake();
    }

    private void Update()
    {
        if (isExit)
        {
            roomPanel.ExitRoomResponse();
            isExit = false;
        }
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
        isExit = true;
    }
}
