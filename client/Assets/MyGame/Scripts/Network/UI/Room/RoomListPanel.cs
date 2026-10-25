using SocketGameProtocol;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Google.Protobuf.Collections;


public class RoomListPanel : BasePanel
{
    public Button back, find, create;
    public TMP_InputField roomName;
    public Slider roomNum;

    public Transform roomListTransform;
    public GameObject roomItem;

    public CreateRoomRequest createRoomRequest;
    public FindRoomRequest findRoomRequest;
    public JoinRoomRequest joinRoomRequest;

    private void Start()
    {
        back.onClick.AddListener(OnBackClick);
        find.onClick.AddListener(OnFindClick);
        create.onClick.AddListener(OnCreateClick);
    }

    /// <summary>
    /// 注销登录按钮
    /// </summary>
    private void OnBackClick()
    {
        uiManager.PopPanel();
    }

    /// <summary>
    /// 查询房间
    /// </summary>
    private void OnFindClick()
    {
        findRoomRequest.SendRequest();
    }

    /// <summary>
    /// 创建房间
    /// </summary>
    private void OnCreateClick()
    {
        if (roomName.text == "")
        {
            uiManager.ShowMessage("房间名不能为空！");
            return;
        }
        createRoomRequest.SendRequest(roomName.text, (int)roomNum.value);
    }

    public void CreateRoomResponse(MainPack pack)
    {
        switch (pack.Returncode)
        {
            case ReturnCode.Succeed:
                uiManager.ShowMessage("创建成功！");

                // 创建房间成功后缓存当前房间玩家
                CacheBattlePlayers(pack);

                // 更新UI面板 并更新房间列表
                RoomPanel roomPanel = uiManager.PushPanel(PanelType.Room).GetComponent<RoomPanel>();
                roomPanel.UpdatePlayerList(pack);
                break;
            case ReturnCode.Fail:
                uiManager.ShowMessage("创建失败！");
                break;
            default:
                Debug.Log("def");
                break;
        }
    }

    public void FindRoomResponse(MainPack pack)
    {
        switch (pack.Returncode)
        {
            case ReturnCode.Succeed:
                uiManager.ShowMessage("查询成功！一共有" + pack.Roompack.Count + "个房间");
                break;
            case ReturnCode.Fail:
                uiManager.ShowMessage("查询出错！");
                break;
            case ReturnCode.NotRoom:
                uiManager.ShowMessage("当前没有房间！");
                break;
            default:
                Debug.Log("def");
                break;
        }
        UpdateRoomList(pack);
    }

    // 使用Google.Protobuf的RepeatedField列表来存储RoomPack信息包
    private void UpdateRoomList(MainPack pack)
    {
        // 清空房间列表
        for (int i = 0; i < roomListTransform.childCount; i++)
        {
            Destroy(roomListTransform.GetChild(i).gameObject);
        }

        if (pack.Roompack.Count == 0) return;

        foreach (var room in pack.Roompack)
        {
            // 生成房间预制体 并设置父物体
            RoomItem item = Instantiate(roomItem, Vector3.zero, Quaternion.identity).GetComponent<RoomItem>();
            item.gameObject.transform.SetParent(roomListTransform);
            // 赋值给进入房间按钮
            item.roomListPanel = this;

            // 传入房间信息内容
            item.SetRoomInfo(room.Roomname, room.Curnum, room.Maxnum, room.Status);
        }
    }

    public void JoinRoomResponse(MainPack pack)
    {
        switch (pack.Returncode)
        {
            case ReturnCode.Succeed:
                uiManager.ShowMessage("加入房间成功！");

                // 加入房间成功后缓存当前房间玩家
                CacheBattlePlayers(pack);

                // 通过返回值 让PushPanel的时候可以调用到返回的面板中的方法（玩家列表更新）
                RoomPanel roomPanel = uiManager.PushPanel(PanelType.Room).GetComponent<RoomPanel>();
                roomPanel.UpdatePlayerList(pack);
                break;
            case ReturnCode.Fail:
                uiManager.ShowMessage("加入房间失败！");
                break;
            default:
                uiManager.ShowMessage("房间不存在！");
                break;
        }
    }

    public void JoinRoom(string roomName)
    {
        joinRoomRequest.SendRequest(roomName);
    }

    // 统一缓存房间玩家，避免 CreateRoom / JoinRoom 写重复逻辑
    private void CacheBattlePlayers(MainPack pack)
    {
        if (pack == null)
            return;

        if (GameFace.Instance == null)
            return;

        RoomStartGameBridge.Instance.CacheRoomPlayers(pack, GameFace.Instance.LocalPlayerName);
    }

    public override void OnEnter()
    {
        base.OnEnter();
        Enter();
    }

    public override void OnExit()
    {
        base.OnExit();
        Exit();
    }

    public override void OnPause()
    {
        base.OnPause();
        Exit();
    }

    public override void OnRecovery()
    {
        base.OnRecovery();
        Enter();
    }

    private void Enter()
    {
        gameObject.SetActive(true);
    }

    private void Exit()
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        back.onClick.RemoveListener(OnBackClick);
        find.onClick.RemoveListener(OnFindClick);
        create.onClick.RemoveListener(OnCreateClick);
    }
}
