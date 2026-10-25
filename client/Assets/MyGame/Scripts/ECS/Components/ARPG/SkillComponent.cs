using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 技能组件
public struct SkillComponent
{
    public int SkillID;

    public float Cooldown;
    public float CurrentCD;

    public bool IsCasting;
}
