using System;


/// <summary>
/// 背包筛选查询对象
/// 现在先支持：
///    一级大类
///    驱动盘套装筛选
/// </summary>
[Serializable]
public class InventoryFilterQuery
{
    public BackpackCategoryType Category = BackpackCategoryType.None;

    // 驱动盘筛选扩展
    public bool EnableDriveDiscSetFilter = false;
    public DriveDiscSetType DriveDiscSetType = DriveDiscSetType.None;

    // 预留未来扩展
    public bool OnlyUnlocked = false;
}