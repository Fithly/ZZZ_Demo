using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketGameProtocol;
using System.Linq;

public class StartingRequest : BaseRequest
{
    private MainPack isStart = null;

    // OnResponse 来自 Socket 异步线程，Update 在主线程读，增加锁
    private readonly object packLock = new object();

    public override void Awake()
    {
        requestCode = RequestCode.Room;
        actionCode = ActionCode.Starting;
        base.Awake();
    }

    private void Update()
    {
        MainPack pack = null;

        lock (packLock)
        {
            if (isStart != null)
            {
                pack = isStart;
                isStart = null;
            }
        }

        if (pack == null)
            return;

        // 收到 Starting 后，如果服务端带了玩家列表，先刷新房间战斗缓存
        if (pack.Playerpack != null && pack.Playerpack.Count > 0)
        {
            if (GameFace.Instance != null)
            {
                RoomStartGameBridge.Instance.CacheRoomPlayers(
                    pack,
                    GameFace.Instance.LocalPlayerName);
            }
        }

        // 收到 Starting 后，通过启动桥加载 BattleScene
        RoomStartGameBridge.Instance.HandleStarting();
    }

    public override void OnResponse(MainPack pack)
    {
        // 这里只保存包，让 Update 在 Unity 主线程处理场景加载
        lock (packLock)
        {
            isStart = pack;
        }
    }
}
