using UnityEngine;

// 提供开始、停止、查找配置的方法
public static class AttackMotionUtility
{
    // 获得攻击位移配置
    public static bool TryGetClip(AttackMotionComponent motion, int comboIndex, out AttackMotionClip clip)
    {
        clip = null;

        if (motion.Clips == null || motion.Clips.Length == 0)
            return false;

        for (int i = 0; i < motion.Clips.Length; i++)
        {
            // 找到匹配的攻击段数位移
            if (motion.Clips[i] != null && motion.Clips[i].ComboIndex == comboIndex)
            {
                clip = motion.Clips[i];
                return true;
            }
        }

        return false;
    }

    public static void StartMotion(ref AttackMotionComponent motion, int comboIndex, Quaternion rotation,
                                bool overrideForwardDistance = false, float forwardDistanceOverride = 0f)

    {
        motion.IsActive = true;                      // 标记攻击位移开始执行
        motion.ActiveComboIndex = comboIndex;        // 记录当前要执行哪一段连击位移
        motion.Elapsed = 0f;                         // 重置已过时间，从头开始计时
        motion.LastCurveValue = 0f;                  // 重置上一次曲线值
        motion.FrameVelocity = Vector3.zero;         // 先清空上一帧的速度，避免残留

        motion.OverrideForwardDistance = overrideForwardDistance;
        motion.ForwardDistanceOverride = Mathf.Max(0f, forwardDistanceOverride);

        Vector3 dir = rotation * Vector3.forward;    // 根据角色当前旋转，算出“面朝前方”的世界方向
        dir.y = 0f;

        // 防止方向长度太小接近 0
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector3.forward;


        // 把方向标准化成单位向量，存到组件里
        motion.WorldDirection = dir.normalized;
    }

    public static void StopMotion(ref AttackMotionComponent motion)
    {
        motion.IsActive = false;                 // 停止攻击位移，并重置状态
        motion.ActiveComboIndex = 0;             // 清空当前连击编号
        motion.Elapsed = 0f;                     // 清空计时
        motion.LastCurveValue = 0f;              // 清空曲线记录
        motion.FrameVelocity = Vector3.zero;     // 清空帧速度，避免角色继续被旧速度推动

        motion.OverrideForwardDistance = false;
        motion.ForwardDistanceOverride = 0f;
    }
}