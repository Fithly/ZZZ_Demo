using System;

/// <summary>
/// 单独记录“装备贡献了多少属性”，方便调试和 UI 拆分显示
/// </summary>
[Serializable]
public class EquipmentAttributeSummary
{
    public CharacterComputedAttributes WeaponAttributes = new CharacterComputedAttributes();
    public CharacterComputedAttributes DriveDiscAttributes = new CharacterComputedAttributes();
    public CharacterComputedAttributes ExtraAttributes = new CharacterComputedAttributes();
}