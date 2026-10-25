using System;

/// <summary>
/// 物品配置公共字段
/// 供 Json 配置层使用
/// </summary>
[Serializable]
public class ItemConfigCommon
{
    public int ItemId;
    public string Name;
    public string Description;
    public string IconKey;

    public ItemType ItemType;
    public ItemQuality Quality;

    public int MaxStack;
    public bool CanStack;
}