using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AttackComponent
{
    public AttackStageDefinition[] Stages;

    public float CooldownTimer;        // 当前冷却计时
    public float ComboResetTime;       // 连招超时后重置回第一段
    public float InputBufferTime;      // 输入缓冲时间（非常关键）

    public int MaxCombo;               // 最大连段数
}
