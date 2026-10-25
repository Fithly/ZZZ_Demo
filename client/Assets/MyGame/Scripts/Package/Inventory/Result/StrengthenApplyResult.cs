using System;

/// <summary>
/// 强化应用结果
/// </summary>
[Serializable]
public class StrengthenApplyResult
{
    public bool Success;
    public string ErrorMessage;

    public int OldLevel;
    public int NewLevel;
    public int OldLevelExp;
    public int NewLevelExp;

    // 消耗数量
    public int ConsumedLowCount;
    public int ConsumedMidCount;
    public int ConsumedHighCount;

    public static StrengthenApplyResult Fail(string error)
    {
        return new StrengthenApplyResult
        {
            Success = false,
            ErrorMessage = error
        };
    }

    public static StrengthenApplyResult Ok()
    {
        return new StrengthenApplyResult
        {
            Success = true
        };
    }
}