using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单次受击请求
/// 由 DamageSystem 在成功扣血且目标未死亡时生成
/// </summary>
public struct HitReactionRequestInfo
{
    public Vector3 HitDirection;     // 受击方向
    public float StunDuration;       // 眩晕持续时间
    public float KnockbackSpeed;     // 击退速度
    public float KnockbackDamping;   // 击退阻尼
}

/// <summary>
/// 一帧内可能收到多个受击请求
/// </summary>
public struct HitReactionBufferComponent
{
    public List<HitReactionRequestInfo> Requests;
}

/// <summary>
/// 持续性受击状态
/// </summary>
public struct HitReactionStateComponent
{
    public bool IsInHitStun;                 // 处于受击硬直状态
    public float HitStunTimer;               // 受击硬直时间
                                             
    public Vector3 KnockbackVelocity;        // 击退速度向量
    public float KnockbackDamping;           // 击退阻尼
}