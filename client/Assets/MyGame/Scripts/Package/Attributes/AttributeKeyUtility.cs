using System;

/// <summary>
/// 把 "HP" / "Attack" / "Defense" / "CritRate" / "CritDamage" 这种字符串键，安全地映射到属性对象字段上
/// </summary>
public static class AttributeKeyUtility
{
    public const string HP = "生命值";
    public const string Attack = "攻击力";
    public const string Defense = "防御值";
    public const string CritRate = "暴击率";
    public const string CritDamage = "暴击伤害";

    public static void Apply(CharacterComputedAttributes attributes, ItemAttributeData attributeData)
    {
        if (attributes == null)
            throw new ArgumentNullException(nameof(attributes));

        switch (attributeData.Key)
        {
            case HP:
                attributes.HP += attributeData.Value;
                break;
            case Attack:
                attributes.Attack += attributeData.Value;
                break;
            case Defense:
                attributes.Defense += attributeData.Value;
                break;
            case CritRate:
                attributes.CritRate += attributeData.Value;
                break;
            case CritDamage:
                attributes.CritDamage += attributeData.Value;
                break;
        }
    }
}