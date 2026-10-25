using SocketGameProtocol;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamePanel : BasePanel
{
    public GameObject item;
    public Transform listTransform;
    public TextMeshProUGUI timerText;
    public Button exitBtn;

    private float startTimer;

    private Dictionary<string, PlayerInfoItem> itemList = new Dictionary<string, PlayerInfoItem>();

    private void Start()
    {
        startTimer = Time.time;
        exitBtn.onClick.AddListener(OnExitClick);
    }

    // 刷新列表
    public void UpdateList(List<PlayerPack> packs)
    {
        foreach (var p in packs)
        {
            GameObject g = Instantiate(item, Vector3.zero, Quaternion.identity);
            g.transform.SetParent(listTransform);

            PlayerInfoItem info = g.GetComponent<PlayerInfoItem>();
            // 更新UI
            info.Set(p.Playername, p.Hp);
            // 存入字典
            itemList.Add(p.PlayerID, info);
        }
    }

    public void UpdateValue(string id, int v)
    {
        if (itemList.TryGetValue(id, out PlayerInfoItem info))
        {
            info.Up(v);
        }
        else
        {
            Debug.Log("获取不到对应的角色信息");
        }
    }

    private void FixedUpdate()
    {
        // 限制范围
        timerText.text = Mathf.Clamp((int)(Time.time - startTimer), 0, 300).ToString();
    }

    private void OnExitClick()
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
}
