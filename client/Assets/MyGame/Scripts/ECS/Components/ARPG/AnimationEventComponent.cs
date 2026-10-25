using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 动画事件 -> ECS 的回传信号
/// </summary>
public struct AnimationEventComponent
{
    public bool HitFrame;            // 命中帧
    public bool ComboWindowOpen;     // 预输入打开
    public bool ComboWindowClose;    // 预输入关闭
    public bool AttackFinished;      // 攻击结束

    public bool RunEndFinished;      // Run_End 播放结束
}