using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 动画表现参数（持续状态）
/// 不再放一次性 Trigger 命令
/// </summary>
public struct AnimationComponent
{
    public float MoveSpeed;      // 用于 Blend Tree 的权重 (0~1)
    public bool IsGrounded;      // 是否在地面
    public bool IsDead;          // 是否死亡
    public bool HasMoveInput;    // 是否可移动
    public bool AttackEnd;

    public bool IsInAttackState; // 是否处于攻击状态

    // 是否允许 Locomotion 直接回到 Idle
    public bool CanEnterIdle;
}