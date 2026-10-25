using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 怪物网络身份组件
/// 
/// 职责：
/// 1. 标记当前怪物的网络ID
/// 2. 标记当前客户端是否负责驱动这个怪物
/// 
/// 第一版规则：
/// 房主驱动怪物 AI / 伤害 / 死亡。
/// 非房主只接收房主同步的怪物表现。
/// </summary>
public struct MonsterNetworkIdentityComponent
{
    public string MonsterId;
    public bool IsHostControlled;
}