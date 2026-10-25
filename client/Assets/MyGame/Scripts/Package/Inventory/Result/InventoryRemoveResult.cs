using System;
using System.Collections.Generic;

/// <summary>
/// ÒÆ³ý½á¹û
/// </summary>

[Serializable]
public class InventoryRemoveResult
{
    public bool Success;
    public int ItemId;
    public int RequestedCount;
    public int RemovedCount;
    public int RemainingNeedCount;

    public List<int> ChangedSlotIndices = new List<int>();
}