using System;
using System.Collections.Generic;
using UnityEngine;
using XLua;

/// <summary>
/// xLua 生成配置
/// 
/// 目前 Demo 阶段只暴露少量 Bridge 类型，
/// 不要一开始把大量 ECS / UI / Service 全部暴露给 Lua。
/// </summary>
public static class XLuaGenConfig
{
    // Lua 调 C# 时需要生成适配代码的类型
    [LuaCallCSharp]
    public static List<Type> LuaCallCSharp = new List<Type>
    {
        typeof(LuaDemoBridge),
        typeof(ResultPanel),
        typeof(Debug)
    };

    // C# 从 Lua 里读取函数，并转成 C# 委托时需要配置
    [CSharpCallLua]
    public static List<Type> CSharpCallLua = new List<Type>
    {
        typeof(LuaDemoFunction)
    };

    // IL2CPP 下防裁剪
    [ReflectionUse]
    public static List<Type> ReflectionUse = new List<Type>
    {
        typeof(LuaDemoBridge),
        typeof(ResultPanel)
    };
}

[CSharpCallLua]
public delegate void LuaDemoFunction(LuaDemoBridge bridge);