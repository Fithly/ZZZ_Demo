using System;

// 区分：
// 
// 角色等级升级材料
// 
// 武器等级升级材料
// 
// 驱动盘等级升级材料
// 
// 角色突破材料
// 
// 武器突破材料

[Serializable]
public class MaterialDefinition : ItemDefinition
{
    // 保留用途类型，便于配置校验、筛选扩展
    public MaterialPurposeType PurposeType;
}