using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗场景启动器
/// 
/// 职责：
/// 1. 读取 RoomBattleSession
/// 2. 生成本地玩家和远端玩家
/// 3. 给本地玩家绑定相机
/// 
/// 第一轮只负责“进入战斗场景后的多人生成骨架”
/// 不负责真正的网络同步
/// </summary>
public class BattleSceneBootstrap : MonoBehaviour
{
    [Header("Player Prefab")]
    [SerializeField] private PlayerView localPlayerPrefab;
    [SerializeField] private PlayerView remotePlayerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();  // 玩家生成点

    [Header("Camera")]
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;

    [Header("HP")]
    [SerializeField] private UIHealthBar uiHealthBar;

    // 战斗网络入口
    [Header("Network")]
    [SerializeField] private NetworkBattleEntry networkBattleEntry;

    private void Start()
    {
        // 自动查找 NetworkBattleEntry，减少场景拖引用遗漏
        if (networkBattleEntry == null)
            networkBattleEntry = FindObjectOfType<NetworkBattleEntry>();

        // 没有房间缓存时，允许单机调试
        if (!RoomBattleSession.Instance.HasValidPlayers())
        {
            SpawnSingleDebugPlayer();
            return;
        }

        SpawnRoomPlayers();
    }

    private void SpawnRoomPlayers()
    {
        // 获取房间玩家
        IReadOnlyList<RoomBattlePlayerData> players = RoomBattleSession.Instance.Players;

        for (int i = 0; i < players.Count; i++)
        {
            RoomBattlePlayerData playerData = players[i];
            if (playerData == null)
                continue;

            // 获取生成点信息
            Transform spawnPoint = GetSpawnPoint(i);
            Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

            // 判断使用哪个预制体
            PlayerView prefab = playerData.IsLocalPlayer
                ? localPlayerPrefab
                : (remotePlayerPrefab != null ? remotePlayerPrefab : localPlayerPrefab);

            if (prefab == null)
                continue;

            PlayerView playerView = Instantiate(prefab, spawnPos, spawnRot);
            playerView.name = playerData.IsLocalPlayer
                ? $"LocalPlayer_{playerData.PlayerName}"
                : $"RemotePlayer_{playerData.PlayerName}";

            if (playerData.IsLocalPlayer)
            {
                playerView.InitializeAsLocal(playerData.PlayerName);
                BindCamera(playerView.transform);
                BindHPBar(playerView);
            }
            else
            {
                playerView.InitializeAsRemote(playerData.PlayerName);
            }

            // 把生成出来的玩家登记到 NetworkBattleEntry
            RegisterBattlePlayer(playerData, playerView);
        }

        // 所有玩家生成完成后，本地玩家通知其他客户端自己已进入战斗
        if (networkBattleEntry != null)
        {
            networkBattleEntry.SendLocalBattleJoin();
        }
    }

    private void SpawnSingleDebugPlayer()
    {
        if (localPlayerPrefab == null)
            return;

        // 获取生成点位置信息
        Transform spawnPoint = GetSpawnPoint(0);
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        PlayerView playerView = Instantiate(localPlayerPrefab, spawnPos, spawnRot);
        playerView.name = "LocalPlayer_Debug";
        playerView.InitializeAsLocal("DebugPlayer");

        BindCamera(playerView.transform);
        BindHPBar(playerView);

        // 单机调试时也登记本地玩家
        if (networkBattleEntry != null)
        {
            networkBattleEntry.RegisterPlayerView("DebugPlayer", 0, true, playerView);
        }
    }

    private void RegisterBattlePlayer(RoomBattlePlayerData playerData, PlayerView playerView)
    {
        if (networkBattleEntry == null)
            return;

        // 登记玩家名、座位号、本地/远端标记、PlayerView引用
        networkBattleEntry.RegisterPlayerView(
            playerData.PlayerName,
            playerData.SeatIndex,
            playerData.IsLocalPlayer,
            playerView);
    }


    private Transform GetSpawnPoint(int index)
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
            return null;

        if (index < 0)
            index = 0;

        index = Mathf.Clamp(index, 0, spawnPoints.Count - 1);
        return spawnPoints[index];
    }

    private void BindCamera(Transform target)
    {
        ThirdPersonCamera cam = thirdPersonCamera != null
            ? thirdPersonCamera
            : FindObjectOfType<ThirdPersonCamera>();

        // 把相机绑定到目标身上
        if (cam != null)
            cam.target = target;
    }

    private void BindHPBar(PlayerView player)
    {
        UIHealthBar bar = uiHealthBar != null
            ? uiHealthBar 
            : FindObjectOfType<UIHealthBar>();

        if(bar != null)
            player.playerHealthBar = uiHealthBar;
    }
}