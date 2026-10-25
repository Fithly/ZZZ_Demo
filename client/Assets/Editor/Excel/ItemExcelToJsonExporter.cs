#if UNITY_EDITOR
using System.Data;
using System.IO;
using ExcelDataReader;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Excel 导 Json 主入口
/// 读取 xlsx
/// 按列名生成三张 Json
/// 自动写入 Resources/Configs
/// 
/// 弹出文件选择框
/// 读取.xlsx
/// 找三张 Sheet
/// 调 parser 生成配置对象
/// 写出三张 Json
/// 刷新 Unity 资源数据库
/// </summary>
public static class ItemExcelToJsonExporter
{
    // Tool中导入的按钮
    [MenuItem(ItemExcelExportConstants.ExportMenuPath)]
    public static void Export()
    {
        string excelPath = EditorUtility.OpenFilePanel("Select Item Excel File", Application.dataPath, "xlsx");
        if (string.IsNullOrEmpty(excelPath))
            return;

        ExportFromExcel(excelPath);
    }

    public static void ExportFromExcel(string excelPath)
    {
        if (string.IsNullOrEmpty(excelPath) || !File.Exists(excelPath))
        {
            Debug.LogError($"[ItemExcelToJsonExporter] Excel file not found. Path={excelPath}");
            return;
        }

        // ===== 修改：ExcelDataReader 读取 xlsx 前注册编码提供器 =====
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        using (FileStream stream = File.Open(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
        {
            DataSet dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true // ===== 修改：按首行列名读取 =====
                }
            });

            if (dataSet == null)
            {
                Debug.LogError("[ItemExcelToJsonExporter] Failed to read Excel DataSet.");
                return;
            }

            DataTable weaponTable = GetSheet(dataSet, ItemExcelExportConstants.WeaponSheetName);
            DataTable driveDiscTable = GetSheet(dataSet, ItemExcelExportConstants.DriveDiscSheetName);
            DataTable materialTable = GetSheet(dataSet, ItemExcelExportConstants.MaterialSheetName);

            // ===== 修改：正式导出前先进行校验 =====
            if (!ItemExcelExportValidator.ValidateWeaponSheet(weaponTable))
                return;

            if (!ItemExcelExportValidator.ValidateDriveDiscSheet(driveDiscTable))
                return;

            if (!ItemExcelExportValidator.ValidateMaterialSheet(materialTable))
                return;

            WeaponDefinitionConfigList weaponConfigs = ItemExcelSheetParser.ParseWeaponSheet(weaponTable);
            DriveDiscDefinitionConfigList driveDiscConfigs = ItemExcelSheetParser.ParseDriveDiscSheet(driveDiscTable);
            MaterialDefinitionConfigList materialConfigs = ItemExcelSheetParser.ParseMaterialSheet(materialTable);

            ItemExcelJsonFileWriter.WriteWeaponJson(weaponConfigs);
            ItemExcelJsonFileWriter.WriteDriveDiscJson(driveDiscConfigs);
            ItemExcelJsonFileWriter.WriteMaterialJson(materialConfigs);
            ItemExcelJsonFileWriter.RefreshAssetDatabase();

            Debug.Log(
                $"[ItemExcelToJsonExporter] Export success. " +
                $"Weapon={weaponConfigs.Items.Count}, " +
                $"DriveDisc={driveDiscConfigs.Items.Count}, " +
                $"Material={materialConfigs.Items.Count}");
        }
    }

    private static DataTable GetSheet(DataSet dataSet, string sheetName)
    {
        if (dataSet == null || string.IsNullOrEmpty(sheetName))
            return null;

        for (int i = 0; i < dataSet.Tables.Count; i++)
        {
            DataTable table = dataSet.Tables[i];
            if (table != null && table.TableName == sheetName)
                return table;
        }

        Debug.LogWarning($"[ItemExcelToJsonExporter] Sheet not found. SheetName={sheetName}");
        return null;
    }
}
#endif