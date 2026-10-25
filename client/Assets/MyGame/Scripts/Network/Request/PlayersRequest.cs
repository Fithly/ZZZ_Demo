using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketGameProtocol;

/// <summary>
/// 收到服务端 PlayerList 广播时，不只刷新房间 UI，还同步刷新 RoomBattleSession。
/// 这样房主开始游戏时，所有客户端都有最新玩家列表。
/// </summary>
public class PlayersRequest : BaseRequest
{
    private MainPack pack = null;
    public RoomPanel roomPanel;

    public override void Awake()
    {
        // 不需要发送 只需要接收即可 则不用设置requestCode
        actionCode = ActionCode.PlayerList;
        base.Awake();
    }

    private void Update()
    {
        if (pack != null)
        {
            // 收到玩家列表广播时，先缓存战斗玩家列表
            CacheBattlePlayers(pack);

            // 再刷新房间内玩家UI
            roomPanel.UpdatePlayerList(pack);
            pack = null;
        }
    }

    public override void OnResponse(MainPack pack)
    {
        this.pack = pack;
    }

    // 统一处理房间玩家缓存
    private void CacheBattlePlayers(MainPack pack)
    {
        if (pack == null)
            return;

        if (GameFace.Instance == null)
            return;

        RoomStartGameBridge.Instance.CacheRoomPlayers(pack, GameFace.Instance.LocalPlayerName);
    }
}
