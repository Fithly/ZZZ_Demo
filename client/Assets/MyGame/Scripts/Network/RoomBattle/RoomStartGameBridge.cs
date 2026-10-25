using UnityEngine;
using UnityEngine.SceneManagement;
using SocketGameProtocol;

/// <summary>
/// 房间开始游戏桥接器
/// 
/// 职责：
/// 1. 缓存房间玩家列表
/// 2. 收到 Starting 后切到战斗场景
/// 
/// 注意：
/// 这个脚本本身不负责收网络包。
/// 由现有的“客户端接包分发脚本”在对应分支里调用它。
/// </summary>
public class RoomStartGameBridge : Singleton<RoomStartGameBridge>
{
    [Header("Battle Scene")]
    [SerializeField] private string battleSceneName = "BattleScene";

    /// <summary>
    /// 缓存当前房间玩家
    /// 建议在以下几个分支都调用：
    /// 1. CreateRoom 成功
    /// 2. JoinRoom 成功
    /// 3. 收到 PlayerList 广播
    /// </summary>
    public void CacheRoomPlayers(MainPack pack, string localPlayerName)
    {
        if (pack == null)
            return;

        RoomBattleSession.Instance.SetRoomPlayers(pack.Playerpack, localPlayerName);
    }

    /// <summary>
    /// 收到服务端 Starting 后，正式进入战斗场景
    /// </summary>
    public void HandleStarting()
    {
        if (!RoomBattleSession.Instance.HasValidPlayers())
        {
            Debug.LogWarning("[RoomStartGameBridge] 没有有效房间玩家缓存，无法进入战斗场景。");
            return;
        }

        SceneManager.LoadScene(battleSceneName);
    }
}