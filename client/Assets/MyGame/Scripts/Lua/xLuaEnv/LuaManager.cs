using System;
using System.IO;
using UnityEngine;
using XLua;

/// <summary>
/// Lua 运行环境管理器
/// 
/// 职责：
/// 1. 创建唯一 LuaEnv
/// 2. 注册自定义 Loader
/// 3. 支持热更目录 persistentDataPath/Lua
/// 4. 支持首包目录 StreamingAssets/Lua
/// 5. 支持编辑器调试目录 DataPath/Lua
/// 6. 统一 Tick 和释放
/// </summary>
public class LuaManager
{
    private static LuaManager instance;

    public static LuaManager Instance
    {
        get
        {
            if (instance == null)
                instance = new LuaManager();

            return instance;
        }
    }

    private LuaEnv luaEnv;
    private float tickTimer = 0f;

    private const float LuaGCTickInterval = 1f;

    // Lua全局表
    public LuaTable Global
    {
        get
        {
            return luaEnv != null ? luaEnv.Global : null;
        }
    }

    private LuaManager()
    {
    }

    // 初始化唯一 LuaEnv
    public void Initialize()
    {
        if (luaEnv != null)
            return;

        luaEnv = new LuaEnv();
        luaEnv.AddLoader(ProjectLoader);

        Debug.Log("[LuaManager] LuaEnv 初始化完成.");
    }

    private byte[] ProjectLoader(ref string filePath)
    {
        string relativePath = filePath.Replace(".", "/").Replace("\\", "/") + ".lua";

        // 热更目录
        string hotfixPath = Path.Combine(Application.persistentDataPath, "Lua", relativePath);
        if (File.Exists(hotfixPath))
            return File.ReadAllBytes(hotfixPath);

        // 首包目录
        string streamingPath = Path.Combine(Application.streamingAssetsPath, "Lua", relativePath);
        if (File.Exists(streamingPath))
            return File.ReadAllBytes(streamingPath);

        // 编辑器调试目录
#if UNITY_EDITOR
        string editorPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../DataPath/Lua", relativePath));
        if (File.Exists(editorPath))
            return File.ReadAllBytes(editorPath);
#endif

        Debug.LogWarning($"[LuaManager] Lua 文件未找到: {relativePath}");
        return null;
    }

    // 执行Lua语句
    public object[] DoString(string code)
    {
        if (luaEnv == null)
            Initialize();

        try
        {
            return luaEnv.DoString(code);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LuaManager] 执行 Lua 失败: {ex}");
            return null;
        }
    }

    public void Tick(float deltaTime)
    {
        if (luaEnv == null)
            return;

        tickTimer += deltaTime;
        if (tickTimer < LuaGCTickInterval)
            return;

        tickTimer = 0f;
        luaEnv.Tick();
    }

    // 释放Lua
    public void Dispose()
    {
        if (luaEnv == null)
            return;

        try
        {
            luaEnv.Dispose();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LuaManager] 释放 LuaEnv 失败: {ex}");
        }

        luaEnv = null;
        instance = null;

        Debug.Log("[LuaManager] LuaEnv 已释放.");
    }
}