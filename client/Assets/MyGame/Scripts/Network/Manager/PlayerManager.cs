using SocketGameProtocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : BaseManager
{
    public PlayerManager(GameFace face) : base(face) { }

    // 存储玩家
    Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();

    private GameObject character;
    private Transform spawnPos;

    public override void OnInit()
    {
        base.OnInit();
        character = Resources.Load("Prefab/Character") as GameObject;
    }

    public string CurPlayerID 
    { 
        get; set; 
    }

    public void AddPlayer(List<PlayerPack> pack)
    {
        spawnPos = GameObject.Find("SpawnPos").transform;
        foreach (var p in pack)
        {
            // 实例化
            GameObject g = GameObject.Instantiate(character, spawnPos.position, Quaternion.identity);

            // 如果等于当前客户端
            if (p.Playername.Equals(CurPlayerID))
            {
                // 创建本地角色

            }
            // 创建其他客户端的角色


            players.Add(p.Playername, g);
        }
    }

    public void RemovePlayer(string id)
    {
        if (players.TryGetValue(id, out GameObject g))
        {
            GameObject.Destroy(g);
            players.Remove(id);
        }
        else
        {
            Debug.Log("移除角色出错！");
        }
    }
}
