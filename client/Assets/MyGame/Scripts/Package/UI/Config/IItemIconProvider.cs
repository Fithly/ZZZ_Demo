using UnityEngine;

/// <summary>
/// 获取图标接口
/// </summary>
public interface IItemIconProvider
{
    Sprite GetIcon(ItemDefinition itemDefinition);
}