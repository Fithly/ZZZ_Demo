#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Excel 行读取工具
/// 按列名安全读取单元格
/// </summary>
public static class ItemExcelRowUtility
{
    public static string GetString(DataRow row, string columnName)
    {
        if (row == null || row.Table == null || !row.Table.Columns.Contains(columnName))
            return string.Empty;

        object value = row[columnName];
        return value == null ? string.Empty : value.ToString().Trim();
    }

    public static int GetInt(DataRow row, string columnName)
    {
        string text = GetString(row, columnName);
        if (string.IsNullOrEmpty(text))
            return 0;

        if (int.TryParse(text, out int value))
            return value;

        Debug.LogWarning($"[ItemExcelRowUtility] Int parse failed. Column={columnName}, Value={text}");
        return 0;
    }

    public static float GetFloat(DataRow row, string columnName, int decimals = 6)
    {
        string text = GetString(row, columnName);
        if (string.IsNullOrEmpty(text))
            return 0f;

        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
        {
            // ===== 修改：先做一层四舍五入，降低 Json 导出时的浮点长尾噪声 =====
            return (float)Math.Round(value, decimals);
        }

        if (float.TryParse(text, out value))
        {
            // ===== 修改：兼容本地小数格式 =====
            return (float)Math.Round(value, decimals);
        }

        Debug.LogWarning($"[ItemExcelRowUtility] Float parse failed. Column={columnName}, Value={text}");
        return 0f;
    }

    public static bool GetBool(DataRow row, string columnName)
    {
        string text = GetString(row, columnName);
        if (string.IsNullOrEmpty(text))
            return false;

        text = text.Trim().ToLower();

        // ===== 修改：兼容策划表中常见写法 =====
        if (text == "1" || text == "true" || text == "yes" || text == "y" || text == "是")
            return true;

        if (text == "0" || text == "false" || text == "no" || text == "n" || text == "否")
            return false;

        Debug.LogWarning($"[ItemExcelRowUtility] Bool parse failed. Column={columnName}, Value={text}");
        return false;
    }

    public static List<ItemAttributeConfig> ReadAttributes(DataRow row, int maxCount)
    {
        var results = new List<ItemAttributeConfig>();
        if (row == null)
            return results;

        for (int i = 1; i <= maxCount; i++)
        {
            string key = GetString(row, $"Attr{i}Key");
            if (string.IsNullOrEmpty(key))
                continue;

            float value = GetFloat(row, $"Attr{i}Value");

            results.Add(new ItemAttributeConfig
            {
                Key = key,
                Value = value
            });
        }

        return results;
    }

    public static List<string> ReadMinorKeys(DataRow row, int maxCount)
    {
        var results = new List<string>();
        if (row == null)
            return results;

        for (int i = 1; i <= maxCount; i++)
        {
            string key = GetString(row, $"Minor{i}Key");
            if (string.IsNullOrEmpty(key))
                continue;

            results.Add(key);
        }

        return results;
    }
}
#endif