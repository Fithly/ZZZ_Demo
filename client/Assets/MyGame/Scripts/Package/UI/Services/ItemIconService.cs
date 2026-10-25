using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 获取图标 让UI层不直接访问资源路径
/// 
/// 1. 优先从 AB 包读取图标
/// 2. 找不到时回退 Resources
/// 3. 保留缓存，避免重复加载
/// </summary>
public class ItemIconService : IItemIconProvider
{
    private const string IconBundleName = "ui_icons";

    private readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public Sprite GetIcon(ItemDefinition itemDefinition)
    {
        if (itemDefinition == null)
            return null;

        if (string.IsNullOrEmpty(itemDefinition.IconKey))
            return null;

        if (cache.TryGetValue(itemDefinition.IconKey, out var cachedSprite))
            return cachedSprite;

        Sprite sprite = LoadFromAssetBundle(itemDefinition.IconKey);

        // AB 中没有时，回退 Resources，避免一次性迁移资源导致旧 UI 全部丢图。
        if (sprite == null)
            sprite = Resources.Load<Sprite>($"Icons/{itemDefinition.IconKey}");

        if (sprite != null)
            cache.Add(itemDefinition.IconKey, sprite);

        return sprite;
    }

    private Sprite LoadFromAssetBundle(string iconKey)
    {
        if (string.IsNullOrEmpty(iconKey))
            return null;

        return AssetBundleManager.Instance.LoadAsset<Sprite>(IconBundleName, iconKey);
    }

    public void ClearCache()
    {
        cache.Clear();
    }
}