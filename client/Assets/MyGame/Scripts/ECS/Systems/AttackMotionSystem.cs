using UnityEngine;

/// <summary>
/// 负责把“攻击位移曲线”转换成每帧速度
/// </summary>
public class AttackMotionSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.entities)
        {
            if (!entity.Has<AttackMotionComponent>() || !entity.Has<HealthComponent>())
                continue;

            var motion = entity.Get<AttackMotionComponent>();
            var health = entity.Get<HealthComponent>();

            // 每帧开始先清零速度
            // 这样如果这帧没有攻击位移，就不会残留上一帧速度
            motion.FrameVelocity = Vector3.zero;

            // 如果死亡
            if (health.IsDead)
            {
                AttackMotionUtility.StopMotion(ref motion);
                entity.Add(motion);
                continue;
            }

            // 没有激活攻击位移
            if (!motion.IsActive)
            {
                entity.Add(motion);
                continue;
            }

            // 尝试根据当前连击编号，找到对应的位移配置
            AttackMotionClip clip;
            if (!AttackMotionUtility.TryGetClip(motion, motion.ActiveComboIndex, out clip))
            {
                // 如果找不到，说明配置有问题，不能继续跑
                AttackMotionUtility.StopMotion(ref motion);
                entity.Add(motion);
                continue;
            }

            // 累计攻击已经持续的时间
            motion.Elapsed += deltaTime;

            // 先定义一个标准化时间变量
            float normalizedTime = 1f;
            if (clip.Duration > 0.0001f)
            {
                // 将Time限制在0-1之间 用已过时间除以动作持续时间
                normalizedTime = Mathf.Clamp01(motion.Elapsed / clip.Duration);
            }

            // 先默认曲线值等于时间进度
            float curveValue = normalizedTime;
            // 如果有曲线，就用曲线计算“当前位移完成比例”
            if (clip.DistanceCurve != null)
            {
                curveValue = clip.DistanceCurve.Evaluate(normalizedTime);
            }

            // 把曲线结果也限制在 0~1，防止曲线配得太夸张导致超范围
            curveValue = Mathf.Clamp01(curveValue);

            // deltaCurve：这一帧曲线前进了多少
            // 然后更新“上一帧曲线值”为当前值
            float deltaCurve = curveValue - motion.LastCurveValue;
            motion.LastCurveValue = curveValue;

            // 做安全判断，避免除以 0，也避免极小误差带来抖动
            if (deltaTime > 0.0001f && Mathf.Abs(deltaCurve) > 0.000001f)
            {
                // 前进距离是否被覆盖
                float forwardDistance = motion.OverrideForwardDistance
                    ? motion.ForwardDistanceOverride
                    : clip.ForwardDistance;

                // 根据“曲线推进比例”算出这一帧应该移动多少距离
                float deltaDistance = forwardDistance * deltaCurve;
                // 把“本帧位移”除以“帧时间”，换算成速度 （速度 = 位移 / 时间）
                motion.FrameVelocity = motion.WorldDirection * (deltaDistance / deltaTime);
            }

            // 如果时间已经走完，说明这段攻击位移完成了，就把它标记为结束
            // 不调用StopMotion 这样当前帧算完后，下一帧就不会继续推进了
            if (normalizedTime >= 1f)
            {
                motion.IsActive = false;
                motion.OverrideForwardDistance = false;
                motion.ForwardDistanceOverride = 0f;
            }

            entity.Add(motion);
        }
    }
}