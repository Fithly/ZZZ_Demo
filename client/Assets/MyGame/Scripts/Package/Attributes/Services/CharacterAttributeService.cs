using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

/// <summary>
/// 读取玩家基础属性
///
/// 读取已装备音擎属性
/// 
/// 读取已装备驱动盘属性
/// 
/// 读取实例额外属性
/// 
/// 生成最终属性结果
/// </summary>
public class CharacterAttributeService
{
    private readonly ItemDefinitionRepository itemRepository;

    public CharacterAttributeService(ItemDefinitionRepository itemRepository)
    {
        this.itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
    }

    // 生成最终属性结果
    public CharacterComputedAttributes BuildFinalAttributes(GameSaveData saveData)
    {
        ValidateSaveData(saveData);

        var finalAttributes = BuildBaseAttributes(saveData.PlayerAttributeData);

        ApplyWeaponDefinitionAttributes(saveData, finalAttributes);
        ApplyDriveDiscDefinitionAttributes(saveData, finalAttributes);
        ApplyWeaponExtraAttributes(saveData, finalAttributes);
        ApplyDriveDiscExtraAttributes(saveData, finalAttributes);

        return finalAttributes;
    }

    // 单独记录“装备贡献了多少属性”
    public EquipmentAttributeSummary BuildEquipmentSummary(GameSaveData saveData)
    {
        ValidateSaveData(saveData);

        var summary = new EquipmentAttributeSummary();

        ApplyWeaponDefinitionAttributes(saveData, summary.WeaponAttributes);
        ApplyDriveDiscDefinitionAttributes(saveData, summary.DriveDiscAttributes);
        ApplyWeaponExtraAttributes(saveData, summary.ExtraAttributes);
        ApplyDriveDiscExtraAttributes(saveData, summary.ExtraAttributes);

        return summary;
    }

    // 创建玩家基础属性
    public CharacterComputedAttributes BuildBaseAttributes(PlayerAttributeData playerAttributeData)
    {
        if (playerAttributeData == null)
            throw new ArgumentNullException(nameof(playerAttributeData));

        return new CharacterComputedAttributes
        {
            HP = playerAttributeData.BaseHP,
            Attack = playerAttributeData.BaseAttack,
            Defense = playerAttributeData.BaseDefense,
            CritRate = playerAttributeData.BaseCritRate,
            CritDamage = playerAttributeData.BaseCritDamage
        };
    }

    // 获得武器定义并申请获得属性
    private void ApplyWeaponDefinitionAttributes(GameSaveData saveData, CharacterComputedAttributes target)
    {
        // 空对象武器槽也视为“未装备”
        if (!IsValidEquippedItem(saveData.EquippedWeapon))
            return;

        var weaponDefinition = itemRepository.GetAs<WeaponDefinition>(saveData.EquippedWeapon.ItemId);
        if (weaponDefinition == null)
            return;

        ApplyAttributeList(target, weaponDefinition.Attributes);
    }

    // 获取驱动盘槽位不为空的驱动盘属性
    private void ApplyDriveDiscDefinitionAttributes(GameSaveData saveData, CharacterComputedAttributes target)
    {
        foreach (var driveDisc in EnumerateEquippedDriveDiscs(saveData))
        {
            var discDefinition = itemRepository.GetAs<DriveDiscDefinition>(driveDisc.ItemId);
            ApplyAttributeList(target, discDefinition.Attributes);
        }
    }

    // 应用武器额外属性
    private void ApplyWeaponExtraAttributes(GameSaveData saveData, CharacterComputedAttributes target)
    {
        // ===== 修改：空对象武器槽也视为“未装备” =====
        if (!IsValidEquippedItem(saveData.EquippedWeapon))
            return;

        ApplyAttributeList(target, saveData.EquippedWeapon.ExtraAttributes);
    }

    // 应用驱动盘额外属性
    private void ApplyDriveDiscExtraAttributes(GameSaveData saveData, CharacterComputedAttributes target)
    {
        foreach (var driveDisc in EnumerateEquippedDriveDiscs(saveData))
        {
            ApplyAttributeList(target, driveDisc.ExtraAttributes);
        }
    }

    // 将属性列表对对象进行添加
    private void ApplyAttributeList(CharacterComputedAttributes target, List<ItemAttributeData> attributes)
    {
        if (target == null || attributes == null)
            return;

        for (int i = 0; i < attributes.Count; i++)
        {
            AttributeKeyUtility.Apply(target, attributes[i]);
        }
    }

    // 返回槽位不为空的驱动盘
    private IEnumerable<ItemInstanceData> EnumerateEquippedDriveDiscs(GameSaveData saveData)
    {
        // ===== 修改：只返回有效装备实例 =====
        if (IsValidEquippedItem(saveData.DriveDiscEquipData.One)) yield return saveData.DriveDiscEquipData.One;
        if (IsValidEquippedItem(saveData.DriveDiscEquipData.Two)) yield return saveData.DriveDiscEquipData.Two;
        if (IsValidEquippedItem(saveData.DriveDiscEquipData.Three)) yield return saveData.DriveDiscEquipData.Three;
        if (IsValidEquippedItem(saveData.DriveDiscEquipData.Four)) yield return saveData.DriveDiscEquipData.Four;
        if (IsValidEquippedItem(saveData.DriveDiscEquipData.Five)) yield return saveData.DriveDiscEquipData.Five;
        if (IsValidEquippedItem(saveData.DriveDiscEquipData.Six)) yield return saveData.DriveDiscEquipData.Six;
    }

    // ===== 修改：新增统一判断“是否为有效已装备物品” =====
    private bool IsValidEquippedItem(ItemInstanceData item)
    {
        if (item == null)
            return false;

        if (item.ItemId <= 0)
            return false;

        return true;
    }

    private void ValidateSaveData(GameSaveData saveData)
    {
        if (saveData == null)
            throw new ArgumentNullException(nameof(saveData));

        if (saveData.PlayerAttributeData == null)
            throw new InvalidOperationException("PlayerAttributeData is null.");

        if (saveData.DriveDiscEquipData == null)
            throw new InvalidOperationException("DriveDiscEquipData is null.");
    }
}