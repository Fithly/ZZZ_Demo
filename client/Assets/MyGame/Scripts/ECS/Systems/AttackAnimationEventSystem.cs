using UnityEngine;

/// <summary>
/// 处理动画事件回传：
/// - 连招窗口开关
/// - 攻击结束后决定是否续段
/// </summary>
public class AttackAnimationEventSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.entities)
        {
            if (!entity.Has<AnimationEventComponent>() ||
                !entity.Has<AttackRuntimeComponent>() ||
                !entity.Has<AttackComponent>() ||
                !entity.Has<AnimationCommandComponent>() ||
                !entity.Has<TransformComponent>() ||
                !entity.Has<HealthComponent>())
                continue;

            var evt = entity.Get<AnimationEventComponent>();
            var runtime = entity.Get<AttackRuntimeComponent>();
            var attack = entity.Get<AttackComponent>();
            var cmd = entity.Get<AnimationCommandComponent>();
            var trans = entity.Get<TransformComponent>();
            var health = entity.Get<HealthComponent>();

            if (health.IsDead)
            {
                evt.ComboWindowOpen = false;
                evt.ComboWindowClose = false;
                evt.AttackFinished = false;
                entity.Add(evt);
                continue;
            }

            // 1. 连招窗口开启：允许玩家按下一发
            if (evt.ComboWindowOpen)
            {
                runtime.CanQueueNext = true;
                evt.ComboWindowOpen = false;
            }

            // 2. 连招窗口关闭：如果不接连招，后续就无法连击了
            if (evt.ComboWindowClose)
            {
                runtime.CanQueueNext = false;
                evt.ComboWindowClose = false;
            }

            // 3. 动作结束点
            if (evt.AttackFinished)
            {
                evt.AttackFinished = false;

                // 有连招缓存，并且没到最后一段 -> 直接续下一段
                if (runtime.ComboQueued && runtime.CurrentComboIndex < attack.MaxCombo)
                {
                    runtime.CurrentComboIndex++;               // 下一段攻击索引
                    runtime.ComboQueued = false;               // 取消缓存
                    runtime.CanQueueNext = false;              // 下一段还没开始允许
                    runtime.DamageAppliedThisAction = false;   // 下一段伤害还没开始（重置）
                    runtime.IsAttacking = true;                // 依旧处于攻击状态

                    // 传输给View动画 开启下一段攻击
                    cmd.AttackEnd = false;
                    cmd.TriggerAttack = true; 
                    cmd.AttackIndex = runtime.CurrentComboIndex;

                    if (entity.Has<AttackMotionComponent>())
                    {
                        var motion = entity.Get<AttackMotionComponent>();
                        AttackMotionUtility.StartMotion(ref motion, runtime.CurrentComboIndex, trans.Rotation);
                        entity.Add(motion);
                    }
                }
                else
                {
                    cmd.TriggerAttack = false;
                    cmd.AttackEnd = true;

                    // 本次连段结束
                    runtime.IsAttacking = false;                            // 退出攻击状态
                    runtime.ComboQueued = false;                            // 取消缓存
                    runtime.CanQueueNext = false;                           // 取消允许
                    runtime.DamageAppliedThisAction = false;                // 取消伤害
                    runtime.ComboResetTimer = attack.ComboResetTime;        // 赋值冷却

                    // 如果已经到最后一段，直接归零
                    if (runtime.CurrentComboIndex >= attack.MaxCombo)
                    {
                        runtime.ComboResetTimer = 0;
                    }

                    // 攻击彻底结束，恢复普通移动
                    trans.CanMove = true;

                    // 停止攻击位移
                    if (entity.Has<AttackMotionComponent>())
                    {
                        var motion = entity.Get<AttackMotionComponent>();
                        AttackMotionUtility.StopMotion(ref motion);
                        entity.Add(motion);
                    }
                }
            }

            entity.Add(evt);
            entity.Add(runtime);
            entity.Add(cmd);
            entity.Add(trans);
        }
    }
}