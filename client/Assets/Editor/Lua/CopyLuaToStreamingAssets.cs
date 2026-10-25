using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Lua 首包复制工具
/// 
/// 将 DataPath/Lua 复制到 Assets/StreamingAssets/Lua。
/// 打包前执行一次，保证首包内有 Lua 文件。
/// </summary>
public static class CopyLuaToStreamingAssets
{
    [MenuItem("Lua/复制Lua到StreamingAssets")]
    public static void Copy()
    {
        string projectRoot = Application.dataPath.Substring(0, Application.dataPath.Length - 7);

        string sourcePath = Path.Combine(projectRoot, "DataPath/Lua");
        string targetPath = Path.Combine(Application.dataPath, "StreamingAssets/Lua");

        if (!Directory.Exists(sourcePath))
        {
            Debug.LogError($"[CopyLuaToStreamingAssets] 源目录不存在: {sourcePath}");
            return;
        }

        if (Directory.Exists(targetPath))
            Directory.Delete(targetPath, true);

        CopyDirectory(sourcePath, targetPath);

        AssetDatabase.Refresh();

        Debug.Log($"[CopyLuaToStreamingAssets] Lua 复制完成: {targetPath}");
    }

    private static void CopyDirectory(string sourcePath, string targetPath)
    {
        Directory.CreateDirectory(targetPath);

        string[] files = Directory.GetFiles(sourcePath);
        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];

            // 不复制 meta
            if (file.EndsWith(".meta"))
                continue;

            string fileName = Path.GetFileName(file);
            string targetFile = Path.Combine(targetPath, fileName);

            File.Copy(file, targetFile, true);
        }

        string[] directories = Directory.GetDirectories(sourcePath);
        for (int i = 0; i < directories.Length; i++)
        {
            string directory = directories[i];

            string folderName = Path.GetFileName(directory);
            string targetDirectory = Path.Combine(targetPath, folderName);

            CopyDirectory(directory, targetDirectory);
        }
    }
}