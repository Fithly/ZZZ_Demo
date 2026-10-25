using System.Collections.Generic;
using Google.Protobuf.Collections;
using SocketGameProtocol;

/// <summary>
/// 房间到战斗场景的运行时缓存
/// 
/// 职责：
/// 1. 缓存当前房间玩家列表
/// 2. 记录本地玩家名
/// 3. 供 BattleScene 读取生成玩家
/// </summary>
public class RoomBattleSession
{
    private static readonly RoomBattleSession instance = new RoomBattleSession();
    public static RoomBattleSession Instance => instance;

    // 缓存房间中玩家列表
    private readonly List<RoomBattlePlayerData> players = new List<RoomBattlePlayerData>();

    public string LocalPlayerName { get; private set; }

    public IReadOnlyList<RoomBattlePlayerData> Players => players;
    public int PlayerCount => players.Count;

    private RoomBattleSession()
    {
    }

    // 清空玩家列表
    public void Clear()
    {
        players.Clear();
        LocalPlayerName = string.Empty;
    }

    // 设置本地玩家名字
    public void SetLocalPlayerName(string playerName)
    {
        LocalPlayerName = playerName ?? string.Empty;
    }

    // 设置房间玩家（信息包PlayerPack）
    public void SetRoomPlayers(RepeatedField<PlayerPack> playerPacks, string localPlayerName)
    {
        players.Clear();
        LocalPlayerName = localPlayerName ?? string.Empty;

        if (playerPacks == null)
            return;

        for (int i = 0; i < playerPacks.Count; i++)
        {
            PlayerPack pack = playerPacks[i];
            if (pack == null)
                continue;

            // 填充房间玩家信息
            players.Add(new RoomBattlePlayerData
            {
                SeatIndex = i,
                PlayerName = pack.Playername,
                IsLocalPlayer = !string.IsNullOrEmpty(LocalPlayerName) && pack.Playername == LocalPlayerName
            });
        }
    }

    // 房间是否有玩家
    public bool HasValidPlayers()
    {
        return players.Count > 0;
    }
}