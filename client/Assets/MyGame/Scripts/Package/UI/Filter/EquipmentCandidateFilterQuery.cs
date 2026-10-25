using System;

/// <summary>
/// 当前装备槽位点击后，小背包候选物品筛选条件
/// 
/// 支持：
/// 
/// 音擎槽 -> 只看音擎
/// 
/// 驱动盘槽 -> 只看该编号驱动盘
/// </summary>
[Serializable]
public class EquipmentCandidateFilterQuery
{
    public EquipmentSlotCategoryType SlotCategory = EquipmentSlotCategoryType.None;
    public DriveDiscSlotType DriveDiscSlotType = DriveDiscSlotType.None;
}