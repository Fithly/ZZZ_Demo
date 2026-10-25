using UnityEngine;

/// <summary>
/// 攻击请求组件：
/// IsRequesting 只表示“本帧刚按下攻击”
/// BufferTimer 表示“这次输入还允许被系统消费多久”
/// </summary>
public struct AttackRequestComponent
{
    public bool IsRequesting;
    public float BufferTimer;
}