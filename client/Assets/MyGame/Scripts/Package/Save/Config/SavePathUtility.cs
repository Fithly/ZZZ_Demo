using System.IO;
using UnityEngine;

/// <summary>
/// 统一生成存档路径，避免路径散落在各处
/// </summary>
public static class SavePathUtility
{
    public static string GetSaveFilePath(string fileName = null)
    {
        string finalFileName = string.IsNullOrEmpty(fileName)
            ? SaveGameConstants.DefaultSaveFileName
            : fileName;

        string path = Path.Combine(Application.persistentDataPath, finalFileName);

        // 支持 Saves/account_xxx/savegame.json 这种带子目录的路径。
        // 写入前确保目录存在。
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        return path;
    }
}