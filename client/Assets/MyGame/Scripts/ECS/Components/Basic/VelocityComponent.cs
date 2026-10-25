using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// MovementSystem 根据 Velocity 修改 Position
public struct VelocityComponent
{
    public Vector3 PlanarVelocity; // 水平速度
    public float VerticalVelocity; // 纵轴速度（重力加速）
    public float MoveSpeed;        // 移动速度
    public float Gravity;          // 重力常数，例如 -9.81f

    // 移动加速度
    // 输入持续按住时，水平速度逐渐靠近 MoveSpeed
    public float Acceleration;

    // 移动减速度
    // 松开输入时，水平速度逐渐回到 0
    public float Deceleration;

    // 速度小于该值时直接归零
    // 防止 Animator MoveSpeed 出现 3.993e-26 这种极小浮点残留
    public float StopSpeedEpsilon;
}