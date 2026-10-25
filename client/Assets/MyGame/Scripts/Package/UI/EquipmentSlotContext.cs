using System;

/// <summary>
/// 装备槽位的数据模型
/// 记录：
/// 
///  槽位类型
///  
///  驱动盘槽位号
///  
///  显示名称
///  
///  当前装备数据
/// </summary>
[Serializable]
public class EquipmentSlotContext
{
    public EquipmentSlotCategoryType SlotCategory;
    public DriveDiscSlotType DriveDiscSlotType;
    //public string DisplayName;
    public ItemInstanceData EquippedItem;

    public bool IsWeaponSlot => SlotCategory == EquipmentSlotCategoryType.Weapon;
    public bool IsDriveDiscSlot => SlotCategory == EquipmentSlotCategoryType.DriveDisc;
}