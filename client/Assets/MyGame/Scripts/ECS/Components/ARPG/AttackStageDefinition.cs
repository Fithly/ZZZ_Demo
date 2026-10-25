using UnityEngine;

[System.Serializable]
public struct AttackStageDefinition
{
    [Header("Damage")]
    public int AttackDamage;        // 伤害值

    [Header("Hit Shape")]
    public float AttackRange;       // 攻击距离
    public float AttackAngle;       // 攻击扇形角度（总角度）

    [Header("Targeting")]
    public float TargetSearchRange;     // 索敌范围
    public bool AutoSnapToTarget;       // 是否自动贴脸
    public float SnapStopDistance;      // 贴脸保持的距离

    [Header("Motion Fallback")]
    public bool UseAttackMotionWhenAutoSnapFails;   // 本段是否在没锁到目标时继续用 AttackMotion

    [Header("Hit Reaction")]
    public float HitStunDuration;        // 本段受击硬直
    public float HitKnockbackSpeed;      // 本段击退速度
    public float HitKnockbackDamping;    // 本段击退衰减

    [Header("Timing")]
    public float CooldownAfterStage;     // 本段冷却
}