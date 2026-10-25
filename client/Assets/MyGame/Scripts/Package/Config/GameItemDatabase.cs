using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏物品数据库入口
/// 负责：
/// 初始化仓库
/// 从 Json 配置读取物品定义
/// 注册到 Repository
/// </summary>
public static class GameItemDatabase
{
    private static bool initialized = false;

    public static ItemDefinitionRepository Repository { get; private set; }

    public static void Initialize()
    {
        if (initialized && Repository != null)
            return;

        Repository = new ItemDefinitionRepository();

        // 改为从 Json 配置加载定义，而不是硬编码注册 =====
        var loadService = new ItemConfigLoadService();
        List<ItemDefinition> definitions = loadService.LoadAllDefinitions();

        Repository.RegisterRange(definitions);

        initialized = true;

        Debug.Log($"[GameItemDatabase] Initialize success. Count={Repository.Count}");
    }

    // 预留一个强制重载入口，方便后面调试配置表时使用
    public static void Reload()
    {
        initialized = false;
        Initialize();
    }
}