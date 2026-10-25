using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单次命中信息
/// </summary>
public struct DamageHitInfo
{
    public int AttackerId;
    public int TargetId;
    public int Damage;

    public Vector3 HitWorldPosition;
    public Vector3 HitDirection;

    public float HitStunDuration;      // 硬直时间
    public float HitKnockbackSpeed;    // 击退速度
    public float HitKnockbackDamping;  // 击退阻尼
}

/// <summary>
/// 目标本帧收到的伤害队列
/// 一个实体一帧内可以收到多条命中
/// </summary>
public struct DamageBufferComponent
{
    public List<DamageHitInfo> Hits;
}