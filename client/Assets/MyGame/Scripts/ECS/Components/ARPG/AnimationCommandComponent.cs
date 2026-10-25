using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ECS -> View 的一次性动画命令
/// </summary>
public struct AnimationCommandComponent
{
    public bool TriggerAttack;  // 触发攻击
    public int AttackIndex;     // 攻击段数

    public bool TriggerDeath;   // 结束触发
    public bool AttackEnd;

    public bool TriggerHit;     // 触发受击

    public bool TriggerRunEnd;
}