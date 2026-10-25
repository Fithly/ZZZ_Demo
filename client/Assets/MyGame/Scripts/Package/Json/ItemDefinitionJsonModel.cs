using System;
using System.Collections.Generic;

/// <summary>
/// 给 Excel -> Json -> Unity 的导表入口做中间模型
/// 
/// 运行时读取 Json 后再转成定义对象
/// </summary>
[Serializable]
public class ItemDefinitionJsonModel
{
    public int Id;
    public string Name;
    public string Description;
    public string IconKey;
    public string ItemType;

    public string DriveDiscSetType;
    public string DriveDiscSlotType;

    public int ExpProvideValue;

    public List<ItemAttributeData> BaseAttributes;
}