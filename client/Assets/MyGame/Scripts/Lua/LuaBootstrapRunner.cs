using UnityEngine;
using XLua;

[CSharpCallLua]
public delegate void LuaLifeCycle();

[GCOptimize]
public struct LuaBootstrapTable
{
    public LuaLifeCycle Start;
    public LuaLifeCycle Update;
    public LuaLifeCycle OnDestroy;
}

/// <summary>
/// Lua 启动器
/// 
/// 放在首场景中，负责启动 Lua 主入口。
/// </summary>
public class LuaBootstrapRunner : MonoBehaviour
{
    private LuaBootstrapTable bootstrap;
    private bool started = false;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);

        LuaManager.Instance.Initialize();
        LuaManager.Instance.DoString("require('Bootstrap')");

        LuaTable global = LuaManager.Instance.Global;
        if (global == null)
        {
            Debug.LogError("[LuaBootstrapRunner] Lua Global 为空.");
            return;
        }

        bootstrap = global.Get<LuaBootstrapTable>("Bootstrap");

        // 执行Lua的Start周期
        if (bootstrap.Start != null)
        {
            bootstrap.Start();
            started = true;
        }
        else
        {
            Debug.LogError("[LuaBootstrapRunner] Bootstrap.Start 未找到.");
        }
    }

    private void Update()
    {
        // 模拟Update每帧执行
        LuaManager.Instance.Tick(Time.deltaTime);

        if (!started)
            return;

        if (bootstrap.Update != null)
            bootstrap.Update();
    }

    private void OnDestroy()
    {
        if (bootstrap.OnDestroy != null)
            bootstrap.OnDestroy();

        // 修改：
        // 先清空 C# 持有的 Lua 委托。
        bootstrap.Start = null;
        bootstrap.Update = null;
        bootstrap.OnDestroy = null;

        // 修改：
        // 当前项目里 LuaDemoBridge / GameUIEntry / LuaBackpackItemGrantTestEntry
        // 也会使用同一个 LuaManager。
        // 这里不要直接 Dispose LuaEnv，避免还有 LuaTable 或 C# 回调未释放时报错。
        //
        // LuaManager.Instance.Dispose();
    }
}