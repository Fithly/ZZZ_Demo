using SocketGameProtocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateRoomRequest : BaseRequest
{
    public RoomListPanel roomListPanel;
    private MainPack pack = null;

    public override void Awake()
    {
        requestCode = RequestCode.Room;
        actionCode = ActionCode.CreateRoom;
        base.Awake(); 
    }

    private void Update()
    {
        if (pack != null)
        {
            roomListPanel.CreateRoomResponse(pack);
            pack = null;
        }
    }

    // 创建房间需要传输参数
    public void SendRequest(string roomName, int maxnum)
    {
        MainPack pack = new MainPack();
        pack.Requestcode = requestCode;
        pack.Actioncode = actionCode;

        RoomPack roomPack = new RoomPack();
        roomPack.Roomname = roomName;
        roomPack.Maxnum = maxnum;

        // Roompack是repeat列表
        pack.Roompack.Add(roomPack);

        base.SendRequest(pack);
    }

    public override void OnResponse(MainPack pack)
    {
        this.pack = pack;
    }
}
