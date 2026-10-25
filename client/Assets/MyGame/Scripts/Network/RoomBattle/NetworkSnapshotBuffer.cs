using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 网络快照缓冲
/// 
/// 职责：
/// 1. 收集远端对象快照
/// 2. 保留一小段历史状态
/// 3. 根据渲染时间在两个快照之间插值
/// 
/// 说明：
/// 这就是远端对象表现层的核心。
/// 比“收到一个状态就 Lerp 到最新点”更稳定。
/// </summary>
public class NetworkSnapshotBuffer
{
    private readonly List<NetworkRemoteSnapshot> snapshots = new List<NetworkRemoteSnapshot>();

    private const int MaxSnapshotCount = 32;

    public int Count
    {
        get { return snapshots.Count; }
    }

    public void AddSnapshot(NetworkRemoteSnapshot snapshot)
    {
        // 防止空ID进入缓冲
        if (string.IsNullOrEmpty(snapshot.NetId))
            return;

        // 如果乱序到达，按 ReceiveTime 的大小顺序插入
        int insertIndex = snapshots.Count;
        for (int i = 0; i < snapshots.Count; i++)
        {
            if (snapshot.ReceiveTime < snapshots[i].ReceiveTime)
            {
                insertIndex = i;
                break;
            }
        }

        snapshots.Insert(insertIndex, snapshot);

        // 限制缓冲长度，防止长时间运行后列表变大
        while (snapshots.Count > MaxSnapshotCount)
        {
            snapshots.RemoveAt(0);
        }
    }

    public bool TrySample(float renderTime, float maxSnapshotAge, out NetworkRemoteSnapshot sample)
    {
        sample = default;

        if (snapshots.Count == 0)
            return false;

        RemoveTooOldSnapshots(renderTime, maxSnapshotAge);

        if (snapshots.Count == 0)
            return false;

        // 只有一个快照时，直接返回它
        if (snapshots.Count == 1)
        {
            sample = snapshots[0];
            return true;
        }

        // 渲染时间比最早快照还早，使用最早快照
        if (renderTime <= snapshots[0].ReceiveTime)
        {
            sample = snapshots[0];
            return true;
        }

        // 在两个快照之间插值
        for (int i = 0; i < snapshots.Count - 1; i++)
        {
            NetworkRemoteSnapshot from = snapshots[i];
            NetworkRemoteSnapshot to = snapshots[i + 1];

            if (renderTime >= from.ReceiveTime && renderTime <= to.ReceiveTime)
            {
                float duration = to.ReceiveTime - from.ReceiveTime;
                float t = 1f;

                if (duration > 0.0001f)
                    t = Mathf.Clamp01((renderTime - from.ReceiveTime) / duration);

                sample = NetworkRemoteSnapshot.Lerp(from, to, t);
                return true;
            }
        }

        // 渲染时间已经超过最新快照，使用最新快照
        sample = snapshots[snapshots.Count - 1];
        return true;
    }

    // 移除过旧的快照
    private void RemoveTooOldSnapshots(float renderTime, float maxSnapshotAge)
    {
        if (snapshots.Count <= 2)
            return;

        float minTime = renderTime - Mathf.Max(0.1f, maxSnapshotAge);

        while (snapshots.Count > 2 && snapshots[1].ReceiveTime < minTime)
        {
            snapshots.RemoveAt(0);
        }
    }

    public void Clear()
    {
        snapshots.Clear();
    }
}