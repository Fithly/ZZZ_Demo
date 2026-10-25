using System;

/// <summary>
/// 查询结果
/// </summary>

[Serializable]
public class InventoryQueryResult
{
    public int ItemId;
    public int TotalCount;            // 总数量
    public bool Exists => TotalCount > 0;
}