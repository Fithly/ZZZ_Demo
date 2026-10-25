using System;
using System.Collections.Generic;

public class Entity
{
    public int ID;

    Dictionary<Type, object> components = new Dictionary<Type, object>();

    // 添加组件
    public void Add<T>(T component)
    {
        components[typeof(T)] = component;
    }

    // 获得组件
    public T Get<T>()
    {
        return (T)components[typeof(T)];
    }

    // 是否存在组件
    public bool Has<T>()
    {
        return components.ContainsKey(typeof(T));
    }

    public bool TryGet<T>(out T component)
    {
        if (components.TryGetValue(typeof(T), out var value))
        {
            component = (T)value;
            return true;
        }

        component = default;
        return false;
    }

    public void Remove<T>()
    {
        components.Remove(typeof(T));
    }
}