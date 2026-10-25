using System;

[Serializable]
public class PlayerBaseData
{
    public string PlayerId;
    public string PlayerName;

    public int Level;
    public int EnhanceLevel;        // 强化后的等级
    public int Exp;                 // 保留旧字段，兼容现有面板/旧逻辑
    public int CurrentLevelExp;     // 当前等级内经验

    public int Gold;
    public int Diamond;
}