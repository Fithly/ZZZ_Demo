using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 怪物AI组件
// 怪物逻辑：发现玩家 追击 攻击
public enum MonsterAIState
{
    Idle,
    Chase,
    Hit,
    Attack,
    Dead
}

public struct MonsterAIComponent
{
    public MonsterAIState State;
}