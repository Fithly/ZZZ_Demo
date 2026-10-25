using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketGameProtocol;

public class ChatRequest : BaseRequest
{
    private string chatStr = null;

    public RoomPanel roomPanel;

    public override void Awake()
    {
        requestCode = RequestCode.Room;
        actionCode = ActionCode.Chat;
        base.Awake();
    }

    private void Update()
    {
        if (chatStr != null)
        {
            roomPanel.ChatResponse(chatStr);
            chatStr = null;
        }
    }

    public void SendRequest(string str)
    {
        MainPack pack = new MainPack();
        pack.Requestcode = requestCode;
        pack.Actioncode = actionCode;
        pack.Str = str;

        base.SendRequest(pack);
    }

    public override void OnResponse(MainPack pack)
    {
        chatStr = pack.Str;
    }
}
