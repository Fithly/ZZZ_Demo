using System;

/// <summary>
/// 房间进入战斗场景时的玩家数据
/// 第一轮只保留最小信息：
/// 1. 座位号
/// 2. 玩家名
/// 3. 是否本地玩家
/// </summary>
[Serializable]
public class RoomBattlePlayerData
{
    public int SeatIndex;          // 座位号
    public string PlayerName;      // 玩家名
    public bool IsLocalPlayer;     // 是否本地玩家
}