#if UNITY_EDITOR
/// <summary>
/// Excel 导 Json 工具常量
/// </summary>
public static class ItemExcelExportConstants
{
    // Excel Sheet 名
    public const string WeaponSheetName = "weapon_items";
    public const string DriveDiscSheetName = "drive_disc_items";
    public const string MaterialSheetName = "material_items";

    // Json 输出路径
    public const string WeaponJsonOutputPath = "Assets/Resources/Configs/weapon_items.json";
    public const string DriveDiscJsonOutputPath = "Assets/Resources/Configs/drive_disc_items.json";
    public const string MaterialJsonOutputPath = "Assets/Resources/Configs/material_items.json";

    // 编辑器菜单
    public const string ExportMenuPath = "Tools/Item Config/Export Excel To Json";
}
#endif