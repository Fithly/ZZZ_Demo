using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 输入 ≠ 行为
// 输入只是数据
// 行为由 System 处理。
public struct InputComponent
{
    public Vector2 MoveInput; // 移动输入向量 （x： 左右， y： 前后）
    public Vector2 LookInput;

    public bool Attack;
    public bool Skill;
    public bool MouseLook;
    public bool BackPackPanel;
    public bool CharactorPanel;
}