using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 网络远端快照
/// 
/// 职责：
/// 1. 保存某个网络对象在某一帧的状态
/// 2. 供 NetworkSnapshotBuffer 做插值采样
/// 
/// 当前支持：
/// 玩家 / 怪物共用
/// </summary>
public struct NetworkRemoteSnapshot
{
    public string NetId;

    public int Tick;
    public float ReceiveTime;

    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 PlanarVelocity;

    public int CurrentHP;
    public int MaxHP;

    public bool IsDead;
    public bool IsAttacking;
    public int ComboIndex;
    public bool IsInHitStun;

    public static NetworkRemoteSnapshot Lerp(NetworkRemoteSnapshot from, NetworkRemoteSnapshot to, float t)
    {
        NetworkRemoteSnapshot result = new NetworkRemoteSnapshot();

        result.NetId = to.NetId;
        result.Tick = to.Tick;
        result.ReceiveTime = Mathf.Lerp(from.ReceiveTime, to.ReceiveTime, t);

        result.Position = Vector3.Lerp(from.Position, to.Position, t);
        result.Rotation = Quaternion.Slerp(from.Rotation, to.Rotation, t);
        result.PlanarVelocity = Vector3.Lerp(from.PlanarVelocity, to.PlanarVelocity, t);

        // 血量 / 攻击 / 死亡等离散状态使用较新的快照
        result.CurrentHP = to.CurrentHP;
        result.MaxHP = to.MaxHP;
        result.IsDead = to.IsDead;
        result.IsAttacking = to.IsAttacking;
        result.ComboIndex = to.ComboIndex;
        result.IsInHitStun = to.IsInHitStun;

        return result;
    }
}