using System.IO;
using UnityEngine;

/// <summary>
/// 热更目录调试工具
/// 
/// 用于打包后查看 Lua / AB 应该放到哪里。
/// </summary>
public class HotfixPathDebug : MonoBehaviour
{
    private void Start()
    {
        string luaPath = Path.Combine(Application.persistentDataPath, "Lua");
        string abPath = Path.Combine(Application.persistentDataPath, "AB");

        Debug.Log($"[HotfixPathDebug] persistentDataPath={Application.persistentDataPath}");
        Debug.Log($"[HotfixPathDebug] Lua热更目录={luaPath}");
        Debug.Log($"[HotfixPathDebug] AB热更目录={abPath}");

        if (!Directory.Exists(luaPath))
            Directory.CreateDirectory(luaPath);

        if (!Directory.Exists(abPath))
            Directory.CreateDirectory(abPath);
    }
}