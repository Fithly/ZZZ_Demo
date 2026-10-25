using System;
using System.Collections.Generic;

/// <summary>
/// 强化材料选择
/// </summary>
[Serializable]
public class StrengthenMaterialSelection
{
    public int ItemId;              // 物品ID
    public int OwnedCount;          // 拥有数量
    public int SelectedCount;       // 选择数量
    public int ExpPerItem;          // 提供经验值
}

/// <summary>
/// 增强预览结果
/// </summary>
[Serializable]
public class StrengthenPreviewResult
{
    public bool Success;
    public string ErrorMessage;

    public int CurrentLevel;
    public int CurrentLevelExp;

    public int PreviewLevel;            // 预览升级后的等级
    public int PreviewLevelExp;         // 预览到达等级后的经验

    public int AddedExp;
    public int GainedLevelCount;        // 获得的等级数量
    public bool ReachedMaxLevel;        // 到达最大等级

    public List<StrengthenMaterialSelection> MaterialSelections = new List<StrengthenMaterialSelection>();

    public static StrengthenPreviewResult Fail(string error)
    {
        return new StrengthenPreviewResult
        {
            Success = false,
            ErrorMessage = error
        };
    }

    public static StrengthenPreviewResult Ok()
    {
        return new StrengthenPreviewResult
        {
            Success = true
        };
    }
}