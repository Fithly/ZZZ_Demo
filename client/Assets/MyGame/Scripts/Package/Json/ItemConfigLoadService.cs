using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 读取 Json 配置并构建运行时物品定义
/// 默认从 Resources/Configs/ 下读取三张 Json
/// </summary>
public class ItemConfigLoadService
{
    private const string WeaponConfigPath = "Configs/weapon_items";
    private const string DriveDiscConfigPath = "Configs/drive_disc_items";
    private const string MaterialConfigPath = "Configs/material_items";

    public List<ItemDefinition> LoadAllDefinitions()
    {
        var results = new List<ItemDefinition>();

        results.AddRange(LoadWeaponDefinitions());
        results.AddRange(LoadDriveDiscDefinitions());
        results.AddRange(LoadMaterialDefinitions());

        return results;
    }

    public List<WeaponDefinition> LoadWeaponDefinitions()
    {
        var results = new List<WeaponDefinition>();

        TextAsset textAsset = Resources.Load<TextAsset>(WeaponConfigPath);
        if (textAsset == null || string.IsNullOrEmpty(textAsset.text))
        {
            Debug.LogWarning($"[ItemConfigLoadService] Weapon config not found. Path={WeaponConfigPath}");
            return results;
        }

        WeaponDefinitionConfigList configList = JsonUtility.FromJson<WeaponDefinitionConfigList>(textAsset.text);
        if (configList == null || configList.Items == null)
            return results;

        for (int i = 0; i < configList.Items.Count; i++)
        {
            WeaponDefinitionConfig config = configList.Items[i];
            if (config == null)
                continue;

            results.Add(BuildWeaponDefinition(config));
        }

        return results;
    }

    public List<DriveDiscDefinition> LoadDriveDiscDefinitions()
    {
        var results = new List<DriveDiscDefinition>();

        TextAsset textAsset = Resources.Load<TextAsset>(DriveDiscConfigPath);
        if (textAsset == null || string.IsNullOrEmpty(textAsset.text))
        {
            Debug.LogWarning($"[ItemConfigLoadService] DriveDisc config not found. Path={DriveDiscConfigPath}");
            return results;
        }

        DriveDiscDefinitionConfigList configList = JsonUtility.FromJson<DriveDiscDefinitionConfigList>(textAsset.text);
        if (configList == null || configList.Items == null)
            return results;

        for (int i = 0; i < configList.Items.Count; i++)
        {
            DriveDiscDefinitionConfig config = configList.Items[i];
            if (config == null)
                continue;

            results.Add(BuildDriveDiscDefinition(config));
        }

        return results;
    }

    public List<MaterialDefinition> LoadMaterialDefinitions()
    {
        var results = new List<MaterialDefinition>();

        TextAsset textAsset = Resources.Load<TextAsset>(MaterialConfigPath);
        if (textAsset == null || string.IsNullOrEmpty(textAsset.text))
        {
            Debug.LogWarning($"[ItemConfigLoadService] Material config not found. Path={MaterialConfigPath}");
            return results;
        }

        MaterialDefinitionConfigList configList = JsonUtility.FromJson<MaterialDefinitionConfigList>(textAsset.text);
        if (configList == null || configList.Items == null)
            return results;

        for (int i = 0; i < configList.Items.Count; i++)
        {
            MaterialDefinitionConfig config = configList.Items[i];
            if (config == null)
                continue;

            results.Add(BuildMaterialDefinition(config));
        }

        return results;
    }

    private WeaponDefinition BuildWeaponDefinition(WeaponDefinitionConfig config)
    {
        var definition = new WeaponDefinition();

        // ===== 修改：先同步公共字段，再补武器专属字段 =====
        ApplyCommonFields(definition, config, ItemType.Weapon);

        definition.StartNumber = config.StartNumber;
        definition.FunctionName = config.FunctionName;
        definition.BaseAttributionKey = config.BaseAttributionKey;
        definition.AdvanceAttributionKey = config.AdvanceAttributionKey;
        definition.Attributes = BuildAttributes(config.Attributes);

        return definition;
    }

    private DriveDiscDefinition BuildDriveDiscDefinition(DriveDiscDefinitionConfig config)
    {
        var definition = new DriveDiscDefinition();

        ApplyCommonFields(definition, config, ItemType.DriveDisc);

        definition.SetType = config.SetType;
        definition.SlotType = config.SlotType;
        definition.DiscNumber = config.DiscNumber;
        definition.FunctionName = config.FunctionName;
        definition.MainAttributionKey = config.MainAttributionKey;
        definition.MinorAttributionKey = config.MinorAttributionKey != null
            ? new List<string>(config.MinorAttributionKey)
            : new List<string>();
        definition.Attributes = BuildAttributes(config.Attributes);

        return definition;
    }

    private MaterialDefinition BuildMaterialDefinition(MaterialDefinitionConfig config)
    {
        var definition = new MaterialDefinition();

        ApplyCommonFields(definition, config, ItemType.Material);

        definition.PurposeType = config.PurposeType;
        //definition.Attributes = BuildAttributes(config.Attributes);

        return definition;
    }

    private void ApplyCommonFields(ItemDefinition definition, ItemConfigCommon config, ItemType fallbackType)
    {
        if (definition == null || config == null)
            return;

        definition.ItemId = config.ItemId;
        definition.Name = config.Name;
        definition.Description = config.Description;

        // ===== 修改：允许策划配置 ItemType，但为空时仍使用当前表类型兜底 =====
        definition.ItemType = config.ItemType != ItemType.None ? config.ItemType : fallbackType;
        definition.Quality = config.Quality;

        definition.IconKey = config.IconKey;
        definition.MaxStack = config.MaxStack;
        definition.CanStack = config.CanStack;
    }

    private List<ItemAttributeData> BuildAttributes(List<ItemAttributeConfig> configs)
    {
        var results = new List<ItemAttributeData>();
        if (configs == null)
            return results;

        for (int i = 0; i < configs.Count; i++)
        {
            if (configs[i] == null)
                continue;

            results.Add(new ItemAttributeData
            {
                Key = configs[i].Key,
                Value = configs[i].Value
            });
        }

        return results;
    }
}