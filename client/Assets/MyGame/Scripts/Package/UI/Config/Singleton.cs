using UnityEngine;

/// <summary>
/// 泛型单例基类，用于需要全局唯一实例的 MonoBehaviour 组件。
/// </summary>
/// <typeparam name="T">子类类型（必须继承 MonoBehaviour）</typeparam>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static bool _isQuitting = false;  // 防止应用退出时重新创建实例

    /// <summary>
    /// 获取单例实例（只读）
    /// </summary>
    public static T Instance
    {
        get
        {
            // 应用正在退出时避免访问，防止重新创建对象导致错误
            if (_isQuitting)
            {
                Debug.LogWarning($"[Singleton] 实例 {typeof(T)} 已被销毁，不再返回新实例。");
                return null;
            }

            // 实例未创建时进行查找或新建
            if (_instance == null)
            {
                _instance = FindObjectOfType<T>();
                if (_instance == null)
                {
                    GameObject singletonObj = new GameObject();
                    _instance = singletonObj.AddComponent<T>();
                    singletonObj.name = typeof(T).ToString() + " (Singleton)";
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 子类可重写的 Awake 方法，用于自定义初始化。
    /// 默认实现会处理实例唯一性，并可选择是否持久化。
    /// </summary>
    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            // 如果需要场景切换时不被销毁，取消下面这行注释
            // DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 子类可重写的 OnApplicationQuit，用于标记退出状态。
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }
}