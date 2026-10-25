using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻击运行时状态
/// </summary>
public struct AttackRuntimeComponent
{
    public bool IsAttacking;              // 当前是否在攻击状态
    public int CurrentComboIndex;         // 当前第几段
    public bool ComboQueued;              // 是否缓存了下一段输入
    public bool CanQueueNext;             // 当前是否允许接下一段
    public bool DamageAppliedThisAction;  // 本段攻击是否已造成过伤害
    public float ComboResetTimer;         // 连招重置倒计时
}