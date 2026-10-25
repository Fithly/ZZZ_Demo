using System;

/// <summary>
/// 最终汇总后的属性结果对象。
/// 以后 UI、战前展示、伤害公式，都可以直接读
/// </summary>

[Serializable]
public class CharacterComputedAttributes
{
    public float HP;
    public float Attack;
    public float Defense;
    public float CritRate;
    public float CritDamage;

    public CharacterComputedAttributes Clone()
    {
        return new CharacterComputedAttributes
        {
            HP = HP,
            Attack = Attack,
            Defense = Defense,
            CritRate = CritRate,
            CritDamage = CritDamage
        };
    }
}