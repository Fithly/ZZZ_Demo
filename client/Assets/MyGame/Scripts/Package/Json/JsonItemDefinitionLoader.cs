using System;
using UnityEngine;

/// <summary>
/// 把Json配置读取进Unity 并注册到Repository
/// </summary>
public static class JsonItemDefinitionLoader
{
    public static void LoadIntoRepository(string resourcesPath, ItemDefinitionRepository repository)
    {
        // 加载Json文件
        var textAsset = Resources.Load<TextAsset>(resourcesPath);
        if (textAsset == null)
        {
            Debug.LogError($"Json config not found: {resourcesPath}");
            return;
        }

        // 反序列化过程
        var collection = JsonUtility.FromJson<ItemDefinitionJsonCollection>(textAsset.text);
        if (collection == null || collection.Items == null)
            return;

        for (int i = 0; i < collection.Items.Count; i++)
        {
            var model = collection.Items[i];
            var definition = CreateDefinition(model);
            if (definition != null)
            {
                repository.Register(definition);
            }
        }
    }

    // 通过中转模型转化物品定义
    private static ItemDefinition CreateDefinition(ItemDefinitionJsonModel model)
    {
        if (model == null)
            return null;

        if (!Enum.TryParse<ItemType>(model.ItemType, out var itemType))
            return null;

        switch (itemType)
        {
            case ItemType.Weapon:
                return new WeaponDefinition
                {
                    ItemId = model.Id,
                    Name = model.Name,
                    Description = model.Description,
                    IconKey = model.IconKey,
                    ItemType = itemType,
                    Attributes = model.BaseAttributes
                };

            case ItemType.DriveDisc:
                Enum.TryParse<DriveDiscSetType>(model.DriveDiscSetType, out var setType);
                Enum.TryParse<DriveDiscSlotType>(model.DriveDiscSlotType, out var slotType);

                return new DriveDiscDefinition
                {
                    ItemId = model.Id,
                    Name = model.Name,
                    Description = model.Description,
                    IconKey = model.IconKey,
                    ItemType = itemType,
                    SetType = setType,
                    SlotType = slotType,
                    Attributes = model.BaseAttributes
                };

            case ItemType.Material:
                return new MaterialDefinition
                {
                    ItemId = model.Id,
                    Name = model.Name,
                    Description = model.Description,
                    IconKey = model.IconKey,
                    ItemType = itemType
                };

            default:
                return new ItemDefinition
                {
                    ItemId = model.Id,
                    Name = model.Name,
                    Description = model.Description,
                    IconKey = model.IconKey,
                    ItemType = itemType
                };
        }
    }
}