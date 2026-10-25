using System;
using System.Collections.Generic;


/// <summary>
/// 装备结果
/// </summary>
/// 
[Serializable]
public class EquipResult
{
    public bool Success;
    public string ErrorMessage;

    public int SourceSlotIndex = -1;
    public List<int> ChangedSlotIndices = new List<int>();

    public bool WeaponChanged;
    public DriveDiscSlotType DriveDiscSlotType = DriveDiscSlotType.None;
}