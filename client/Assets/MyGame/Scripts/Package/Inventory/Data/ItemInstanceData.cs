using System;
using System.Collections.Generic;

/// <summary>
/// 通用实例数据
/// 材料/货币/消耗品：关注 ItemId + Count
/// 武器/驱动盘：关注 Level / CurrentLevelExp / ExtraAttributes / 强化随机结果
/// </summary>
[Serializable]
public class ItemInstanceData
{
    public string InstanceId;                      // 唯一实例ID
    public int ItemId;                             // 对应配置ID
    public int Count;                              // 数量

    public bool IsBound;                           // 是否绑定
    public int EnhanceLevel;                       // 强化等级
    public int Durability;                         // 耐久（没有可置 0）

    public ItemAcquireSourceType AcquireSourceType;
    public string AcquireSourceParam;              // 例如副本id、奖励id

    // 实例中升级获得的额外属性值
    public List<ItemAttributeData> ExtraAttributes = new List<ItemAttributeData>();

    public long CreateTimeTicks;                   // 创建时间

    public bool IsLocked;                          // 锁定物品，后面可以限制被误拆/误删
    public bool IsMarkedDiscardable;               // 可丢弃标记，供“拆解”按钮批量删除

    // ===== 新增：实例成长数据 =====
    public int Level;                              // 当前强化等级
    public int CurrentLevelExp;                    // 当前等级内经验

    /// <summary>
    /// 仅驱动盘使用：记录 4 / 8 / 12 / 15 级时随机命中的副属性 key
    /// 例如：Attack / CritRate / Defense / CritDamage
    /// </summary>
    public List<string> RolledMinorAttributeKeys = new List<string>();
}