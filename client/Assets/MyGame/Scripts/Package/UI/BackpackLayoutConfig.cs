
using System;

/// <summary>
/// 背包布局配置
/// 统一初始容量与按行扩容规则
/// </summary>
public static class BackpackLayoutConfig
{
    public const int DefaultCapacity = 45;
    public const int RowSlotCount = 9;
    public const int ExpandCountPerTime = RowSlotCount;
}