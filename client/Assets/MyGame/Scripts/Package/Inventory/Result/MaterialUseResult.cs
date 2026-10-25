using System;

/// <summary>
/// 是否成功
/// 
/// 使用个数
/// 
/// 获得经验
/// 
/// 错误信息
/// </summary>
[Serializable]
public class MaterialUseResult
{
    public bool Success;
    public int UsedCount;
    public int TotalExpGained;
    public string ErrorMessage;

    public static MaterialUseResult Fail(string errorMessage)
    {
        return new MaterialUseResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }

    public static MaterialUseResult Ok(int usedCount, int totalExpGained)
    {
        return new MaterialUseResult
        {
            Success = true,
            UsedCount = usedCount,
            TotalExpGained = totalExpGained
        };
    }
}