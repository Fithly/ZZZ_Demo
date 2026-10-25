using UnityEngine;

/// <summary>
/// 当前登录账号上下文
/// 
/// 职责：
/// 1. 记录当前账号ID
/// 2. 记录当前账号显示名
/// 3. 给存档系统生成账号专属文件路径
/// </summary>
public static class CurrentAccountSession
{
    public static string AccountId { get; private set; } = string.Empty;
    public static string AccountName { get; private set; } = string.Empty;

    public static bool IsLoggedIn
    {
        get { return !string.IsNullOrEmpty(AccountId); }
    }

    public static void SetLoginUser(string accountId, string accountName)
    {
        AccountId = string.IsNullOrEmpty(accountId)
            ? string.Empty
            : accountId.Trim();

        AccountName = string.IsNullOrEmpty(accountName)
            ? AccountId
            : accountName.Trim();

        Debug.Log($"[CurrentAccountSession] 登录账号已设置. AccountId={AccountId}, AccountName={AccountName}");
    }

    public static void Clear()
    {
        AccountId = string.Empty;
        AccountName = string.Empty;
    }
}