#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

/// <summary>
/// Excel 导表校验器
/// 负责检查 Sheet、列、枚举值、基础数据合法性
/// </summary>
public static class ItemExcelExportValidator
{
    public static bool ValidateWeaponSheet(DataTable table)
    {
        if (!ValidateRequiredColumns(table, GetWeaponRequiredColumns(), ItemExcelExportConstants.WeaponSheetName))
            return false;

        return ValidateCommonRows(table, ItemExcelExportConstants.WeaponSheetName);
    }

    public static bool ValidateDriveDiscSheet(DataTable table)
    {
        if (!ValidateRequiredColumns(table, GetDriveDiscRequiredColumns(), ItemExcelExportConstants.DriveDiscSheetName))
            return false;

        if (!ValidateCommonRows(table, ItemExcelExportConstants.DriveDiscSheetName))
            return false;

        for (int i = 0; i < table.Rows.Count; i++)
        {
            DataRow row = table.Rows[i];
            int itemId = ItemExcelRowUtility.GetInt(row, "ItemId");
            if (itemId <= 0)
                continue;

            if (!IsDefinedEnum<DriveDiscSetType>(ItemExcelRowUtility.GetInt(row, "SetType")))
            {
                Debug.LogError($"[ItemExcelExportValidator] Invalid SetType. Sheet={ItemExcelExportConstants.DriveDiscSheetName}, Row={i + 2}, ItemId={itemId}");
                return false;
            }

            if (!IsDefinedEnum<DriveDiscSlotType>(ItemExcelRowUtility.GetInt(row, "SlotType")))
            {
                Debug.LogError($"[ItemExcelExportValidator] Invalid SlotType. Sheet={ItemExcelExportConstants.DriveDiscSheetName}, Row={i + 2}, ItemId={itemId}");
                return false;
            }
        }

        return true;
    }

    public static bool ValidateMaterialSheet(DataTable table)
    {
        if (!ValidateRequiredColumns(table, GetMaterialRequiredColumns(), ItemExcelExportConstants.MaterialSheetName))
            return false;

        if (!ValidateCommonRows(table, ItemExcelExportConstants.MaterialSheetName))
            return false;

        for (int i = 0; i < table.Rows.Count; i++)
        {
            DataRow row = table.Rows[i];
            int itemId = ItemExcelRowUtility.GetInt(row, "ItemId");
            if (itemId <= 0)
                continue;

            if (!IsDefinedEnum<MaterialPurposeType>(ItemExcelRowUtility.GetInt(row, "PurposeType")))
            {
                Debug.LogError($"[ItemExcelExportValidator] Invalid PurposeType. Sheet={ItemExcelExportConstants.MaterialSheetName}, Row={i + 2}, ItemId={itemId}");
                return false;
            }
        }

        return true;
    }

    private static bool ValidateCommonRows(DataTable table, string sheetName)
    {
        if (table == null)
        {
            Debug.LogError($"[ItemExcelExportValidator] Sheet is null. Sheet={sheetName}");
            return false;
        }

        var itemIdSet = new HashSet<int>();

        for (int i = 0; i < table.Rows.Count; i++)
        {
            DataRow row = table.Rows[i];
            int itemId = ItemExcelRowUtility.GetInt(row, "ItemId");

            if (itemId <= 0)
            {
                Debug.LogError($"[ItemExcelExportValidator] Invalid ItemId. Sheet={sheetName}, Row={i + 2}");
                return false;
            }

            if (!itemIdSet.Add(itemId))
            {
                Debug.LogError($"[ItemExcelExportValidator] Duplicate ItemId. Sheet={sheetName}, Row={i + 2}, ItemId={itemId}");
                return false;
            }

            if (string.IsNullOrEmpty(ItemExcelRowUtility.GetString(row, "Name")))
            {
                Debug.LogError($"[ItemExcelExportValidator] Name is empty. Sheet={sheetName}, Row={i + 2}, ItemId={itemId}");
                return false;
            }

            if (!IsDefinedEnum<ItemType>(ItemExcelRowUtility.GetInt(row, "ItemType")))
            {
                Debug.LogError($"[ItemExcelExportValidator] Invalid ItemType. Sheet={sheetName}, Row={i + 2}, ItemId={itemId}");
                return false;
            }

            if (!IsDefinedEnum<ItemQuality>(ItemExcelRowUtility.GetInt(row, "Quality")))
            {
                Debug.LogError($"[ItemExcelExportValidator] Invalid Quality. Sheet={sheetName}, Row={i + 2}, ItemId={itemId}");
                return false;
            }
        }

        return true;
    }

    private static bool ValidateRequiredColumns(DataTable table, string[] requiredColumns, string sheetName)
    {
        if (table == null)
        {
            Debug.LogError($"[ItemExcelExportValidator] Sheet not found. Sheet={sheetName}");
            return false;
        }

        for (int i = 0; i < requiredColumns.Length; i++)
        {
            string columnName = requiredColumns[i];
            if (!table.Columns.Contains(columnName))
            {
                Debug.LogError($"[ItemExcelExportValidator] Required column missing. Sheet={sheetName}, Column={columnName}");
                return false;
            }
        }

        return true;
    }

    private static bool IsDefinedEnum<TEnum>(int value) where TEnum : Enum
    {
        return Enum.IsDefined(typeof(TEnum), value);
    }

    private static string[] GetCommonRequiredColumns()
    {
        return new[]
        {
            "ItemId",
            "Name",
            "Description",
            "IconKey",
            "ItemType",
            "Quality",
            "MaxStack",
            "CanStack"
        };
    }

    private static string[] GetWeaponRequiredColumns()
    {
        return CombineColumns(
            GetCommonRequiredColumns(),
            new[]
            {
                "StartNumber",
                "FunctionName",
                "BaseAttributionKey",
                "AdvanceAttributionKey"
            });
    }

    private static string[] GetDriveDiscRequiredColumns()
    {
        return CombineColumns(
            GetCommonRequiredColumns(),
            new[]
            {
                "SetType",
                "SlotType",
                "DiscNumber",
                "FunctionName",
                "MainAttributionKey"
            });
    }

    private static string[] GetMaterialRequiredColumns()
    {
        return CombineColumns(
            GetCommonRequiredColumns(),
            new[]
            {
                "PurposeType"
            });
    }

    private static string[] CombineColumns(string[] a, string[] b)
    {
        var results = new string[a.Length + b.Length];
        Array.Copy(a, 0, results, 0, a.Length);
        Array.Copy(b, 0, results, a.Length, b.Length);
        return results;
    }
}
#endif