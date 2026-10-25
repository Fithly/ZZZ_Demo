using System.Text;
using UnityEngine;

/// <summary>
/// 账号存档文件名工具
/// 
/// 将账号ID转换成安全的存档文件名。
/// </summary>
public static class AccountSaveFileNameUtility
{
    private const string SaveFolderName = "Saves";

    public static string GetCurrentAccountSaveFileName()
    {
        if (!CurrentAccountSession.IsLoggedIn)
        {
            Debug.LogWarning("[AccountSaveFileNameUtility] 当前没有登录账号，使用默认存档.");
            return SaveGameConstants.DefaultSaveFileName;
        }

        return GetAccountSaveFileName(CurrentAccountSession.AccountId);
    }

    public static string GetAccountSaveFileName(string accountId)
    {
        string safeAccountId = ToSafeFileName(accountId);

        if (string.IsNullOrEmpty(safeAccountId))
            safeAccountId = "guest";

        return $"{SaveFolderName}/{safeAccountId}/{SaveGameConstants.DefaultSaveFileName}";
    }

    private static string ToSafeFileName(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];

            bool valid =
                char.IsLetterOrDigit(c) ||
                c == '_' ||
                c == '-';

            if (valid)
                builder.Append(c);
            else
                builder.Append('_');
        }

        return builder.ToString();
    }
}