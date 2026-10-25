using UnityEngine;

/// <summary>
/// 攻击系统：
/// 1、攻击起手时，根据当前段配置取 stage
/// 2、如果是玩家控制单位，则用 当前段的 TargetSearchRange 做自动索敌
/// 3、如果找到怪：
///  无视当前面向
///  直接面朝怪
///  计算“贴脸位置”
///  本次攻击位移改为“朝向目标并使用 override ForwardDistance”
/// 4、如果没找到怪：
///  按当前段配置决定是否 fallback 到 AttackMotion
/// </summary>
public class AttackSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        foreach (var entity in world.entities)
        {
            if (!entity.Has<AttackComponent>() ||
                !entity.Has<AttackRequestComponent>() ||
                !entity.Has<AttackRuntimeComponent>() ||
                !entity.Has<AnimationCommandComponent>() ||
                !entity.Has<HealthComponent>() ||
                !entity.Has<TransformComponent>() ||
                !entity.Has<CampComponent>() ||
                !entity.Has<TargetComponent>() ||
                !entity.Has<HitReactionStateComponent>())
                continue;

            var attack = entity.Get<AttackComponent>();
            var request = entity.Get<AttackRequestComponent>();
            var runtime = entity.Get<AttackRuntimeComponent>();
            var cmd = entity.Get<AnimationCommandComponent>();
            var health = entity.Get<HealthComponent>();
            var trans = entity.Get<TransformComponent>();
            var camp = entity.Get<CampComponent>();
            var targetComp = entity.Get<TargetComponent>();
            var hitState = entity.Get<HitReactionStateComponent>();

            // 获得攻击最大连段
            attack.MaxCombo = AttackStageUtility.GetSafeMaxCombo(ref attack);

            // 已死亡
            if (health.IsDead)
            {
                request.IsRequesting = false;
                request.BufferTimer = 0f;

                runtime.IsAttacking = false;
                runtime.ComboQueued = false;
                runtime.CanQueueNext = false;
                runtime.CurrentComboIndex = 0;
                runtime.ComboResetTimer = 0f;
                runtime.DamageAppliedThisAction = false;

                entity.Add(request);
                entity.Add(runtime);
                entity.Add(attack);
                continue;
            }

            // 受击中
            if (hitState.IsInHitStun)
            {
                request.IsRequesting = false;
                request.BufferTimer = 0f;

                runtime.IsAttacking = false;
                runtime.ComboQueued = false;
                runtime.CanQueueNext = false;
                runtime.CurrentComboIndex = 0;
                runtime.ComboResetTimer = 0f;
                runtime.DamageAppliedThisAction = false;

                cmd.TriggerAttack = false;
                cmd.AttackEnd = true;

                entity.Add(request);
                entity.Add(runtime);
                entity.Add(cmd);
                entity.Add(attack);
                continue;
            }

            // 更新冷却
            if (attack.CooldownTimer > 0f)
            {
                attack.CooldownTimer -= deltaTime;
                if (attack.CooldownTimer < 0f)
                    attack.CooldownTimer = 0f;
            }

            // 更新连招重置计时
            if (!runtime.IsAttacking && runtime.CurrentComboIndex > 0)
            {
                runtime.ComboResetTimer -= deltaTime;
                if (runtime.ComboResetTimer <= 0f)
                {
                    runtime.CurrentComboIndex = 0;
                    runtime.ComboQueued = false;
                    runtime.CanQueueNext = false;
                    runtime.ComboResetTimer = 0f;
                }
            }

            // 本帧点击攻击 -> 转成缓冲输入
            if (request.IsRequesting)
            {
                request.IsRequesting = false;
                // 缓冲时间
                request.BufferTimer = Mathf.Max(request.BufferTimer, attack.InputBufferTime);
            }

            // 是否已消耗缓冲
            bool consumedBufferedInput = false;
            // 是否有缓冲输入
            bool hasBufferedInput = request.BufferTimer > 0f;

            if (hasBufferedInput)
            {
                // 情况1：当前不在攻击中 -> 尝试起手
                if (!runtime.IsAttacking)
                {
                    // 冷却结束
                    if (attack.CooldownTimer <= 0f)
                    {
                        int nextCombo;

                        // 最后一段打完后，下次应从第1段开始
                        if (runtime.CurrentComboIndex <= 0 || runtime.CurrentComboIndex >= attack.MaxCombo)
                            nextCombo = 1;
                        else
                            nextCombo = runtime.CurrentComboIndex;

                        // 将攻击段数限制在合法范围内
                        nextCombo = Mathf.Clamp(nextCombo, 1, Mathf.Max(1, attack.MaxCombo));

                        StartAttackStage(
                                world,
                                entity,
                                ref attack,
                                ref runtime,
                                ref cmd,
                                ref trans,
                                ref targetComp,
                                ref camp,
                                nextCombo);

                        consumedBufferedInput = true;
                    }
                }
                // 情况2：当前正在攻击中 -> 如果已进入输入窗口，则缓存下一段
                else if (runtime.CanQueueNext)
                {
                    runtime.ComboQueued = true;
                    consumedBufferedInput = true;
                }
            }

            // 已消耗过缓存输入
            if (consumedBufferedInput)
            {
                request.BufferTimer = 0f;
            }
            else if (request.BufferTimer > 0f)
            {
                request.BufferTimer -= deltaTime;
                if (request.BufferTimer < 0f)
                    request.BufferTimer = 0f;
            }

            entity.Add(request);
            entity.Add(attack);
            entity.Add(runtime);
            entity.Add(cmd);
            entity.Add(trans);
            entity.Add(targetComp);
        }
    }

    private static void StartAttackStage(World world, Entity entity, ref AttackComponent attack, ref AttackRuntimeComponent runtime,
                                            ref AnimationCommandComponent cmd, ref TransformComponent trans, ref TargetComponent targetComp,
                                            ref CampComponent selfCamp, int comboIndex)
    {
        AttackStageDefinition stage;
        if (!AttackStageUtility.TryGetStage(ref attack, comboIndex, out stage))
            return;

        // 需要扭曲攻击位移
        bool useMotion = false;
        bool overrideForwardDistance = false;
        float forwardDistanceOverride = 0f;

        bool isPlayerControlled = entity.Has<InputComponent>() && !entity.Has<MonsterAIComponent>();
        if (isPlayerControlled && stage.AutoSnapToTarget)
        {
            bool foundTarget = TryPrepareAutoTargetMotion(
                world,
                entity,
                ref trans,
                ref targetComp,
                ref selfCamp,
                ref stage,
                out forwardDistanceOverride);

            if (foundTarget)
            {
                useMotion = true;
                overrideForwardDistance = true;
            }
            else
            {
                useMotion = stage.UseAttackMotionWhenAutoSnapFails;
            }
        }
        else
        {
            useMotion = true;
        }

        trans.CanMove = false;

        runtime.IsAttacking = true;
        runtime.CurrentComboIndex = comboIndex;
        runtime.ComboQueued = false;
        runtime.CanQueueNext = false;
        runtime.DamageAppliedThisAction = false;

        cmd.AttackEnd = false;
        cmd.TriggerAttack = true;
        cmd.AttackIndex = comboIndex;

        // 按当前段配置写冷却
        attack.CooldownTimer = Mathf.Max(stage.CooldownAfterStage, 0f);

        // 如果存在攻击位移
        if (entity.Has<AttackMotionComponent>())
        {
            var motion = entity.Get<AttackMotionComponent>();

            // 需要攻击配置
            if (useMotion)
            {
                AttackMotionUtility.StartMotion(
                    ref motion, 
                    comboIndex, 
                    trans.Rotation, 
                    overrideForwardDistance, 
                    forwardDistanceOverride);
            }
            else
            {
                AttackMotionUtility.StopMotion(ref motion);
            }

            entity.Add(motion);
        }
    }

    /// <summary>
    /// 玩家自动索敌：
    /// - 找目标
    /// - 只改变朝向
    /// - 计算本次攻击应前冲多远
    /// - 不直接改 Position
    /// <returns></returns>
    private static bool TryPrepareAutoTargetMotion(World world, Entity self, ref TransformComponent selfTransform, ref TargetComponent targetComp, 
                                                ref CampComponent selfCamp, ref AttackStageDefinition stage, out float forwardDistanceOverride)
    {
        forwardDistanceOverride = 0f;

        float searchRange = Mathf.Max(0f, stage.TargetSearchRange);
        if (searchRange <= 0f)
            return false;

        Entity bestTarget = null;
        float bestDist = float.MaxValue;

        foreach (var other in world.entities)
        {
            if (other == self)
                continue;

            if (!other.Has<TransformComponent>() ||
                !other.Has<CampComponent>() ||
                !other.Has<HealthComponent>())
                continue;

            var otherCamp = other.Get<CampComponent>();
            var otherHealth = other.Get<HealthComponent>();

            if (otherCamp.CampId == selfCamp.CampId)
                continue;

            if (otherHealth.IsDead)
                continue;

            // 获得目标方向
            var otherTransform = other.Get<TransformComponent>();
            Vector3 delta = otherTransform.Position - selfTransform.Position;
            delta.y = 0f;

            // 获得距离
            float dist = delta.magnitude;
            if (dist > searchRange)
                continue;

            // 获得距离最近的目标
            if (dist < bestDist)
            {
                bestDist = dist;
                bestTarget = other;
            }
        }

        if (bestTarget == null)
            return false;

        // 获得目标方向
        var targetTransform = bestTarget.Get<TransformComponent>();
        Vector3 toTarget = targetTransform.Position - selfTransform.Position;
        toTarget.y = 0f;

        // 方向标准化
        Vector3 dir;
        if (toTarget.sqrMagnitude <= 0.0001f)
            dir = selfTransform.Rotation * Vector3.forward;
        else
            dir = toTarget.normalized;

        // 只改变攻击朝向
        selfTransform.Rotation = Quaternion.LookRotation(dir);

        // 目标id
        targetComp.TargetEntityId = bestTarget.ID;

        // 目标前停下距离
        float stopDistance = Mathf.Max(0.2f, stage.SnapStopDistance);
        float requiredDistance = Mathf.Max(0f, bestDist - stopDistance);

        forwardDistanceOverride = requiredDistance;
        return true;
    }
}