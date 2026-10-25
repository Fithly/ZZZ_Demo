using UnityEngine;

// 配置“某一段攻击位移”的参数
[System.Serializable]
public class AttackMotionClip
{
    public string Name;

    [Min(1)]
    public int ComboIndex = 1;

    [Min(0.01f)]
    public float Duration = 0.35f;

    public float ForwardDistance = 1.2f;

    public bool LockMove = true;

    /// <summary>
    /// 0~1 输入，0~1 输出
    /// 表示“当前攻击阶段走完了多少比例的总位移”
    /// 例如：
    /// 前半段陡一点 = 冲得快
    /// 中间平台 = 顿一下
    /// 后半段再涨 = 二次发力
    /// </summary>
    public AnimationCurve DistanceCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
}

// 保存“当前攻击位移运行到哪了”的状态
public struct AttackMotionComponent
{
    public AttackMotionClip[] Clips;

    public bool IsActive;               // 是否活动
    public int ActiveComboIndex;        // 第几段攻击
    public float Elapsed;               // 已过时间
    public float LastCurveValue;        // 记录上一帧曲线的取值

    public Vector3 WorldDirection;      // 世界方向
    public Vector3 FrameVelocity;       // 帧速度

    public bool OverrideForwardDistance;    // 本次攻击是否覆盖默认前进距离
    public float ForwardDistanceOverride;   // 覆盖距离值
}