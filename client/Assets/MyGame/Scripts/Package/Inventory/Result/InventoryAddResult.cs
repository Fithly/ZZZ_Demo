using System;
using System.Collections.Generic;

/// <summary>
/// 添加结果
/// </summary>

[Serializable]
public class InventoryAddResult
{
    public bool Success;
    public int ItemId;
    public int RequestedCount;
    public int AddedCount;
    public int RemainingCount;   // 剩余数量

    public List<int> ChangedSlotIndices = new List<int>();
}