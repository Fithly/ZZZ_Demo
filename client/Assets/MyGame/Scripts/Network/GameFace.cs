using SocketGameProtocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameFace : MonoBehaviour
{
    public static GameFace Instance;

    private ClientManager clientManager;
    private RequestManager requestManager;
    private NetworkUIManager uiManager;
    private PlayerManager playerManager;

    // 记录当前客户端自己的玩家名，RoomBattleSession 判断本地玩家时使用
    public string LocalPlayerName
    {
        get;
        private set;
    }

    private void Awake()
    {
        // 必须在所有网络 / ECS 初始化之前设置
        InitRuntimeSetting();

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 初始化管理器
        uiManager = new NetworkUIManager(this);
        clientManager = new ClientManager(this);
        requestManager = new RequestManager(this);
        playerManager = new PlayerManager(this);    

        uiManager.OnInit();
        clientManager.OnInit();
        requestManager.OnInit();
        playerManager.OnInit();
    }

    // 强制后台运行，解决两个 exe 窗口互相失焦后暂停的问题
    private void InitRuntimeSetting()
    {
        Application.runInBackground = true;

        // 关闭垂直同步，用 targetFrameRate 控制帧率
        // 避免不同机器 / 不同窗口状态下帧率差异过大
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        // 窗口从暂停恢复时，限制最大 deltaTime
        // 防止恢复瞬间一帧时间过长，导致移动 / 网络同步跳变
        Time.maximumDeltaTime = 0.05f;
    }

    // 有些平台或窗口状态切换后可能重置后台运行状态，这里再次确认
    private void OnApplicationFocus(bool hasFocus)
    {
        Application.runInBackground = true;
    }

    // 从暂停恢复时再次确认
    private void OnApplicationPause(bool pause)
    {
        Application.runInBackground = true;
    }

    private void OnDestroy()
    {
        // 防止重复对象被销毁时误清理真正的单例管理器
        if (Instance != this)
            return;

        if (clientManager != null)
            clientManager.OnDestroy();

        if (requestManager != null)
            requestManager.OnDestroy();

        if (uiManager != null)
            uiManager.OnDestroy();

        if (playerManager != null)
            playerManager.OnDestroy();

        Instance = null;
    }

    public void Send(MainPack pack)
    {
        clientManager.Send(pack);
    }

    public void HandleRespone(MainPack pack)
    {
        // 处理
        requestManager.HandleResponse(pack);
    }

    // 增添请求
    public void AddRequest(BaseRequest request)
    {
        requestManager.AddRequest(request);
    }

    // 删除请求
    public void RemoveRequest(ActionCode action)
    {
        requestManager.RemoveRequest(action);
    }

    public void ShowMessage(string str, bool sync = false)
    {
        // 调用UIManager从而获取MessagePanel
        uiManager.ShowMessage(str, sync);
    }

    #region 开始游戏

    public void SetSelfID(string id)
    {
        // 同时保存本地玩家名
        LocalPlayerName = id;

        playerManager.CurPlayerID = id;

        // 提前写入 RoomBattleSession，后续缓存玩家列表时可判断本地玩家
        RoomBattleSession.Instance.SetLocalPlayerName(id);
    }

    public void AddPlayer(List<PlayerPack> packs)
    {
        playerManager.AddPlayer(packs);
    }

    public void RemovePlayer(string id)
    {
        playerManager.RemovePlayer(id);
    }

    #endregion
}
