using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkUIManager : BaseManager
{
    public NetworkUIManager(GameFace face) : base(face) { }

    // 存储实例化好的UI
    private Dictionary<PanelType, BasePanel> panelDict = new Dictionary<PanelType, BasePanel>();
    // 存储各个UI在Resources中的路径
    private Dictionary<PanelType, string> panelPath = new Dictionary<PanelType, string>();

    // 存储当前游戏显示的UI
    private Stack<BasePanel> panelStack = new Stack<BasePanel>();

    // 父Canvas
    private Transform canvasTransform;
    private MessagePanel messagePanel;

    public override void OnInit()
    {
        base.OnInit();
        // Resources文件不能热更新
        // 初始化路径
        InitPanel();
        canvasTransform = GameObject.Find("Canvas").transform;

        PushPanel(PanelType.Message);
        PushPanel(PanelType.Start);
    }

    /// <summary>
    /// 把UI显示在界面上
    /// </summary>
    /// <param name="panelType"></param>
    public BasePanel PushPanel(PanelType panelType)
    {
        if (panelDict.TryGetValue(panelType, out BasePanel panel))
        {
            if (panelStack.Count > 0)
            {
                // 将正在显示的UI暂停
                BasePanel topPanel = panelStack.Peek();
                topPanel.OnPause();
            }

            // 将需要显示的UI启动
            panelStack.Push(panel);
            panel.OnEnter();

            // 添加返回值 让PushPanel的时候可以调用到返回的面板中的方法（玩家列表更新）
            return panel;
        }
        else
        {
            // 获取不到就实例化
            BasePanel panel1 = SpawnPanel(panelType);
            if (panelStack.Count > 0)
            {
                BasePanel topPanel = panelStack.Peek();
                topPanel.OnPause();
            }

            panelStack.Push(panel1);
            panel1.OnEnter();

            return panel1;
        }
    }

    /// <summary>
    /// 关闭当前UI
    /// </summary>
    public void PopPanel()
    {
        if (panelStack.Count == 0) return;

        BasePanel topPanel = panelStack.Pop();
        topPanel.OnExit();

        BasePanel panel = panelStack.Peek();
        panel.OnRecovery();
    }

    /// <summary>
    /// 实例化对应的UI
    /// </summary>
    /// <param name="panelType"></param>
    private BasePanel SpawnPanel(PanelType panelType)
    {
        // 如果路径字典中有值 则实例化
        if (panelPath.TryGetValue(panelType, out string path))
        {
            GameObject g = GameObject.Instantiate(Resources.Load(path), canvasTransform) as GameObject; 

            BasePanel panel = g.GetComponent<BasePanel>();
            panelDict.Add(panelType, panel);

            panel._setUIManager = this;
            return panel;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 初始化UI路径
    /// </summary>
    private void InitPanel()
    {
        string panelpath = "Panel/";
        string[] path = new string[] { "MessagePanel", "StartPanel", "LoginPanel", "LogonPanel", "RoomListPanel", "RoomPanel"};
        panelPath.Add(PanelType.Message, panelpath + path[0]);
        panelPath.Add(PanelType.Start, panelpath + path[1]);
        panelPath.Add(PanelType.Login, panelpath + path[2]);
        panelPath.Add(PanelType.Logon, panelpath + path[3]);
        panelPath.Add(PanelType.RoomList, panelpath + path[4]);
        panelPath.Add(PanelType.Room, panelpath + path[5]);

        string panelpath1 = "UI/";
        string[] path1 = new string[] { "GamePanel"};
        panelPath.Add(PanelType.Game, panelpath1 + path1[0]);
    }

    public void SetMessagePanel(MessagePanel message)
    {
        messagePanel = message;
    }

    public void ShowMessage(string str, bool sync = false)
    {
        messagePanel.ShowMessage(str, sync);
    }
}
