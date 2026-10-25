using UnityEngine;

/// <summary>
/// 移动表现运行时状态：
/// 1. 记录最近一次非零输入方向
/// 2. 记录上一帧是否在移动
/// 3. Run_End 播放期间锁移动/旋转
/// 4. 为急刹车保留一个很短的观察窗口，避免 W->S 被中间 0 帧误判成 Run_End
/// </summary>
public struct LocomotionStateComponent
{
    public Vector2 LastNonZeroMoveInput;   // 最近一次非零输入方向（归一化）
    public bool WasMovingLastFrame;

    public bool IsRunEndPlaying;

    public bool PendingRunEnd;             // 是否处于“等待确认是否真要 Run_End”
    public float PendingRunEndTimer;       // 观察窗口倒计时

    // 只有速度达到奔跑速度后，松开输入才允许触发 Run_End
    // 轻点移动键时，只在 Idle / Walk 之间变化，不触发急停
    public bool HasReachedRunSpeed;
}