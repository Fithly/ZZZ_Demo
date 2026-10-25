using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 网络身份组件
/// 
/// 职责：
/// 1. 标记 ECS 实体对应哪个网络玩家
/// 2. 区分本地玩家 / 远端玩家
/// 3. 供伤害系统广播 BattleHit / BattleDeath 使用
/// </summary>
public struct NetworkIdentityComponent
{
    public string PlayerId;        // 当前阶段先使用玩家名作为网络ID
    public string PlayerName;      // 玩家名

    public bool IsLocalPlayer;     // 是否本地玩家
    public bool IsHost;            // 是否房主
}