using UnityEngine;

public static class AttackStageUtility
{
    // 获得攻击配置
    public static bool TryGetStage(ref AttackComponent attack, int comboIndex, out AttackStageDefinition stage)
    {
        stage = default;

        if (attack.Stages == null || attack.Stages.Length == 0)
            return false;

        // 限制段数范围
        int index = Mathf.Clamp(comboIndex - 1, 0, attack.Stages.Length - 1);
        stage = attack.Stages[index];
        return true;
    }

    // 获得最大攻击段数
    public static int GetSafeMaxCombo(ref AttackComponent attack)
    {
        if (attack.Stages == null || attack.Stages.Length == 0)
            return 1;

        return Mathf.Max(1, attack.Stages.Length);
    }
}