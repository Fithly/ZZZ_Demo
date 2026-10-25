#if UNITY_EDITOR
using System.Collections.Generic;
using System.Data;

/// <summary>
/// Excel Sheet 解析器
/// 把 DataTable 转成配置对象列表
/// 按列名解析 Excel 行 → 构建配置对象
/// </summary>
public static class ItemExcelSheetParser
{
    public static WeaponDefinitionConfigList ParseWeaponSheet(DataTable table)
    {
        var result = new WeaponDefinitionConfigList();
        if (table == null)
            return result;

        for (int i = 0; i < table.Rows.Count; i++)
        {
            DataRow row = table.Rows[i];
            if (row == null)
                continue;

            // 找到对应行的ID
            int itemId = ItemExcelRowUtility.GetInt(row, "ItemId");
            if (itemId <= 0)
                continue;

            // 在同一行构建对象
            var config = new WeaponDefinitionConfig();
            FillCommonFields(config, row);

            config.StartNumber = ItemExcelRowUtility.GetInt(row, "StartNumber");
            config.FunctionName = ItemExcelRowUtility.GetString(row, "FunctionName");
            config.BaseAttributionKey = ItemExcelRowUtility.GetString(row, "BaseAttributionKey");
            config.AdvanceAttributionKey = ItemExcelRowUtility.GetString(row, "AdvanceAttributionKey");
            config.Attributes = ItemExcelRowUtility.ReadAttributes(row, 6);

            result.Items.Add(config);
        }

        return result;
    }

    public static DriveDiscDefinitionConfigList ParseDriveDiscSheet(DataTable table)
    {
        var result = new DriveDiscDefinitionConfigList();
        if (table == null)
            return result;

        for (int i = 0; i < table.Rows.Count; i++)
        {
            DataRow row = table.Rows[i];
            if (row == null)
                continue;

            int itemId = ItemExcelRowUtility.GetInt(row, "ItemId");
            if (itemId <= 0)
                continue;

            var config = new DriveDiscDefinitionConfig();
            FillCommonFields(config, row);

            config.SetType = (DriveDiscSetType)ItemExcelRowUtility.GetInt(row, "SetType");
            config.SlotType = (DriveDiscSlotType)ItemExcelRowUtility.GetInt(row, "SlotType");
            config.DiscNumber = ItemExcelRowUtility.GetInt(row, "DiscNumber");

            config.FunctionName = ItemExcelRowUtility.GetString(row, "FunctionName");
            config.MainAttributionKey = ItemExcelRowUtility.GetString(row, "MainAttributionKey");
            config.MinorAttributionKey = ItemExcelRowUtility.ReadMinorKeys(row, 4);
            config.Attributes = ItemExcelRowUtility.ReadAttributes(row, 8);

            result.Items.Add(config);
        }

        return result;
    }

    public static MaterialDefinitionConfigList ParseMaterialSheet(DataTable table)
    {
        var result = new MaterialDefinitionConfigList();
        if (table == null)
            return result;

        for (int i = 0; i < table.Rows.Count; i++)
        {
            DataRow row = table.Rows[i];
            if (row == null)
                continue;

            int itemId = ItemExcelRowUtility.GetInt(row, "ItemId");
            if (itemId <= 0)
                continue;

            var config = new MaterialDefinitionConfig();
            FillCommonFields(config, row);

            // ===== 修改：材料表当前只导出 PurposeType，不再导出 ExpProvideValue =====
            config.PurposeType = (MaterialPurposeType)ItemExcelRowUtility.GetInt(row, "PurposeType");
            config.Attributes = ItemExcelRowUtility.ReadAttributes(row, 4);

            result.Items.Add(config);
        }

        return result;
    }

    // ===== 修改：统一补齐公共定义字段 =====
    private static void FillCommonFields(ItemConfigCommon config, DataRow row)
    {
        if (config == null || row == null)
            return;

        config.ItemId = ItemExcelRowUtility.GetInt(row, "ItemId");
        config.Name = ItemExcelRowUtility.GetString(row, "Name");
        config.Description = ItemExcelRowUtility.GetString(row, "Description");

        config.ItemType = (ItemType)ItemExcelRowUtility.GetInt(row, "ItemType");
        config.Quality = (ItemQuality)ItemExcelRowUtility.GetInt(row, "Quality");

        config.IconKey = ItemExcelRowUtility.GetString(row, "IconKey");
        config.MaxStack = ItemExcelRowUtility.GetInt(row, "MaxStack");
        config.CanStack = ItemExcelRowUtility.GetBool(row, "CanStack");
    }
}
#endif