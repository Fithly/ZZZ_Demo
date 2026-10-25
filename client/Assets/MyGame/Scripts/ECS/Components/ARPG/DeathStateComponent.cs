using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct DeathStateComponent
{
    public bool HasProcessedDeath;   // 是否已经处理过第一次死亡
    public float DestroyDelay;       // 延迟销毁时间
    public float DestroyTimer;       // 当前计时
}