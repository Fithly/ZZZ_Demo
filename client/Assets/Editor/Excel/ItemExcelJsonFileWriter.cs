#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Json 文件写入工具
/// 负责把已经解析好的配置对象：
///    转成 Json
///    自动创建目录
///    自动写到 Assets/Resources/Configs/
/// </summary>
public static class ItemExcelJsonFileWriter
{
    public static void WriteWeaponJson(WeaponDefinitionConfigList configList)
    {
        WriteJsonFile(ItemExcelExportConstants.WeaponJsonOutputPath, configList);
    }

    public static void WriteDriveDiscJson(DriveDiscDefinitionConfigList configList)
    {
        WriteJsonFile(ItemExcelExportConstants.DriveDiscJsonOutputPath, configList);
    }

    public static void WriteMaterialJson(MaterialDefinitionConfigList configList)
    {
        WriteJsonFile(ItemExcelExportConstants.MaterialJsonOutputPath, configList);
    }

    private static void WriteJsonFile<T>(string outputPath, T data)
    {
        if (string.IsNullOrEmpty(outputPath))
            return;

        string directory = Path.GetDirectoryName(outputPath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string json = JsonUtility.ToJson(data, true);

        // ===== 修改：对 Json 中过长的小数尾巴做统一整理 =====
        json = NormalizeFloatJson(json, 6);

        File.WriteAllText(outputPath, json);

        Debug.Log($"[ItemExcelJsonFileWriter] Json write success. Path={outputPath}");
    }

    private static string NormalizeFloatJson(string json, int decimals)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        return Regex.Replace(json, @"(-?\d+\.\d{7,})", match =>
        {
            string raw = match.Value;

            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                double rounded = Math.Round(value, decimals);
                return rounded.ToString($"0.{new string('#', decimals)}", CultureInfo.InvariantCulture);
            }

            return raw;
        });
    }

    public static void RefreshAssetDatabase()
    {
        AssetDatabase.Refresh();
    }
}
#endif