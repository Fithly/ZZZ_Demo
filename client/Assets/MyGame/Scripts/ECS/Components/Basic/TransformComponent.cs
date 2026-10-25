using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 存储逻辑位置
// 不直接使用 GameObject.transform
public struct TransformComponent
{
    public Vector3 Position;
    public Quaternion Rotation;
    public bool IsGrounded;        // 是否在地面
    public bool CanMove;           // 是否可移动
    public bool CanRotate;         // 是否允许因移动输入而旋转
}