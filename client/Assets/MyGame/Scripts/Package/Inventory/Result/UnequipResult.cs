using System;
using System.Collections.Generic;

/// <summary>
/// п╤ть╫А╧Ш
/// </summary>

[Serializable]
public class UnequipResult
{
    public bool Success;
    public string ErrorMessage;

    public List<int> ChangedSlotIndices = new List<int>();

    public bool WeaponChanged;
    public DriveDiscSlotType DriveDiscSlotType = DriveDiscSlotType.None;
}