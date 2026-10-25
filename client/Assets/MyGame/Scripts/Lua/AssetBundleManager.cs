using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// AssetBundle 管理器
/// 
/// 职责：
/// 1. 自动寻找 AB 根目录
/// 2. 加载主 Manifest
/// 3. 自动加载依赖
/// 4. 缓存已加载 AB
/// 5. 给 C# 和 Lua 提供统一资源加载接口
/// </summary>
public class AssetBundleManager
{
    private static AssetBundleManager instance;

    public static AssetBundleManager Instance
    {
        get
        {
            if (instance == null)
                instance = new AssetBundleManager();

            return instance;
        }
    }

    private readonly Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();

    private string bundleRootPath;               // ab包根目录
    private AssetBundleManifest manifest;        // 依赖
    private bool initialized = false;            // 是否初始化

    private AssetBundleManager()
    {
    }

    public void Initialize(string customRootPath = null)
    {
        if (initialized)
            return;

        bundleRootPath = string.IsNullOrEmpty(customRootPath)
            ? FindDefaultBundleRootPath()
            : customRootPath;

        LoadManifest();

        initialized = true;
        Debug.Log($"[AssetBundleManager] 初始化完成. Path={bundleRootPath}");
    }

    // 寻找默认ab根目录
    private string FindDefaultBundleRootPath()
    {
        string hotfixPath = Path.Combine(Application.persistentDataPath, "AB");
        if (IsValidBundleRootPath(hotfixPath))
            return hotfixPath;

        string streamingPath = Path.Combine(Application.streamingAssetsPath, "AB");
        if (IsValidBundleRootPath(streamingPath))
            return streamingPath;

#if UNITY_EDITOR
        string editorPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../DataPath/AB"));
        if (IsValidBundleRootPath(editorPath))
            return editorPath;
#endif
        
        // 都找不到时仍然返回热更目录，方便打印错误路径，
        // 但不会再因为空文件夹挡住后面的回退路径。
        return hotfixPath;
    }

    // 只有目录存在，并且里面有主 AB 包时，才认为这个 AB 根目录有效。
    private bool IsValidBundleRootPath(string rootPath)
    {
        if (string.IsNullOrEmpty(rootPath))
            return false;

        if (!Directory.Exists(rootPath))
            return false;

        string upperMainBundle = Path.Combine(rootPath, "AB");
        if (File.Exists(upperMainBundle))
            return true;

        string lowerMainBundle = Path.Combine(rootPath, "ab");
        if (File.Exists(lowerMainBundle))
            return true;

        return false;
    }

    private void LoadManifest()
    {
        string manifestPath = GetMainBundlePath();

        if (string.IsNullOrEmpty(manifestPath) || !File.Exists(manifestPath))
        {
            Debug.LogError($"[AssetBundleManager] 主 AB 包不存在. Path={manifestPath}");
            return;
        }

        AssetBundle mainBundle = AssetBundle.LoadFromFile(manifestPath);
        if (mainBundle == null)
        {
            Debug.LogError($"[AssetBundleManager] 主 AB 包加载失败. Path={manifestPath}");
            return;
        }

        // 加载依赖
        manifest = mainBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        mainBundle.Unload(false);

        if (manifest == null)
            Debug.LogError("[AssetBundleManager] AssetBundleManifest 加载失败.");
    }

    private string GetMainBundlePath()
    {
        string upperPath = Path.Combine(bundleRootPath, "AB");
        if (File.Exists(upperPath))
            return upperPath;

        string lowerPath = Path.Combine(bundleRootPath, "ab");
        if (File.Exists(lowerPath))
            return lowerPath;

        return upperPath;
    }

    public T LoadAsset<T>(string bundleName, string assetName) where T : UnityEngine.Object
    {
        UnityEngine.Object asset = LoadAsset(bundleName, assetName, typeof(T));
        return asset as T;
    }

    public UnityEngine.Object LoadAsset(string bundleName, string assetName, Type assetType)
    {
        if (string.IsNullOrEmpty(bundleName) || string.IsNullOrEmpty(assetName))
            return null;

        if (!initialized)
            Initialize();

        AssetBundle bundle = LoadBundle(bundleName);
        if (bundle == null)
            return null;

        UnityEngine.Object asset = bundle.LoadAsset(assetName, assetType);
        if (asset == null)
            Debug.LogWarning($"[AssetBundleManager] 资源未找到. Bundle={bundleName}, Asset={assetName}");

        return asset;
    }

    // 加载ab包
    private AssetBundle LoadBundle(string bundleName)
    {
        if (loadedBundles.TryGetValue(bundleName, out var cachedBundle))
            return cachedBundle;

        if (manifest != null)
        {
            // 获得所有依赖并逐个加载
            string[] dependencies = manifest.GetAllDependencies(bundleName);
            for (int i = 0; i < dependencies.Length; i++)
            {
                LoadBundle(dependencies[i]);
            }
        }

        string bundlePath = Path.Combine(bundleRootPath, bundleName);
        if (!File.Exists(bundlePath))
        {
            Debug.LogError($"[AssetBundleManager] AB 包不存在. Path={bundlePath}");
            return null;
        }

        AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
        if (bundle == null)
        {
            Debug.LogError($"[AssetBundleManager] AB 包加载失败. Path={bundlePath}");
            return null;
        }

        loadedBundles.Add(bundleName, bundle);
        return bundle;
    }

    // 卸载某个ab包
    public void UnloadBundle(string bundleName, bool unloadAllLoadedObjects = false)
    {
        if (string.IsNullOrEmpty(bundleName))
            return;

        if (!loadedBundles.TryGetValue(bundleName, out var bundle))
            return;

        bundle.Unload(unloadAllLoadedObjects);
        loadedBundles.Remove(bundleName);
    }

    // 卸载所有ab包
    public void UnloadAll(bool unloadAllLoadedObjects = false)
    {
        foreach (var pair in loadedBundles)
        {
            if (pair.Value != null)
                pair.Value.Unload(unloadAllLoadedObjects);
        }

        loadedBundles.Clear();
    }
}