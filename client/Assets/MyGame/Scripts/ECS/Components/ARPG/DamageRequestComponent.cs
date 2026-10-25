using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 受击事件组件
/// </summary>
public struct DamageRequestComponent
{
    public int AttackerId;  // 发起攻击者
    public int TargetId;    // 受击者
    public int Damage;      // 伤害值
}
