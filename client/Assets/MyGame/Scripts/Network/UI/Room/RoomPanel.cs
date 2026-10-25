using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using SocketGameProtocol;

public class RoomPanel : BasePanel
{
    public Button back, send, start;
    public TMP_InputField inputText;
    public Scrollbar scrollbar;

    public TextMeshProUGUI chatText;
    public Transform content;

    // 预制体
    public GameObject userItem;

    // 请求
    public ExitRoomRequest exitRoomRequest;
    public ChatRequest chatRequest;
    public StartGameRequest startGameRequest;

    private void Start()
    {
        back.onClick.AddListener(OnBackClick);
        send.onClick.AddListener(OnSendClick);
        start.onClick.AddListener(OnStartClick);
    }

    private void OnBackClick()
    {
        exitRoomRequest.SendRequest();
    }

    private void OnSendClick()
    {
        if (inputText.text == "")
        {
            uiManager.ShowMessage("发送内容不能为空！");
            return;
        }
        chatRequest.SendRequest(inputText.text);
        chatText.text += "我：" + inputText.text + "\n";
        inputText.text = "";
    }

    private void OnStartClick()
    {
        startGameRequest.SendRequest();
    }

    /// <summary>
    /// 刷新玩家列表
    /// </summary>
    /// <param name="pack"></param>
    public void UpdatePlayerList(MainPack pack)
    {
        // 清空
        for (int i = 0; i < content.childCount; i++)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        foreach (PlayerPack player in pack.Playerpack)
        {
            // 实例化
            UserItem useritem = Instantiate(userItem, Vector3.zero, Quaternion.identity).GetComponent<UserItem>();
            // 设置父物体
            useritem.gameObject.transform.SetParent(content);
            // 传入信息
            useritem.SetPlayerInfo(player.Playername);
        }
    }

    // UI退出房间
    public void ExitRoomResponse()
    {
        uiManager.PopPanel();
    }

    public void ChatResponse(string str)
    {
        chatText.text += str + "\n";
    }

    public void StartGameResponse(MainPack pack)
    {
        switch (pack.Returncode)
        {
            case ReturnCode.Fail:
                uiManager.ShowMessage("开始游戏失败！您不是房主！");
                break;
            case  ReturnCode.Succeed:
                uiManager.ShowMessage("游戏已开始！");
                break;
        }
    }

    public void GameStarting(List<PlayerPack> packs)
    {



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
    }
}
