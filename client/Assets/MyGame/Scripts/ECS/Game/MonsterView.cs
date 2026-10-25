using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MonsterView : MonoBehaviour
{
    // === 面板调参区 ===
    [Header("Move")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float gravity = -9.8f;

    [Header("Health")]
    [SerializeField] private int maxHP = 80;
    [SerializeField] private int currentHP = 80;

    [Header("Attack")]
    [SerializeField] private AttackStageDefinition[] attackStages;
    [SerializeField] private float comboResetTime = 0.8f;          // 连招重置时间
    [SerializeField] private float inputBufferTime = 0.2f;         // 输入缓冲时间

    [Header("Attack Motion")]
    [SerializeField] private AttackMotionClip[] attackMotionClips;

    [Header("Targeting")]
    [SerializeField] private float searchRange = 8f;

    [Header("Camp")]
    [SerializeField] private int campId = 2;

    [Header("Death")]
    [SerializeField] private float destroyDelay = 2.0f;

    [Header("UI")]
    [SerializeField] private UIHealthBar monsterHealthBar;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 2.2f, 0f);

    [Header("Network")]
    [SerializeField] private string networkMonsterId = "";

    // 只有房主负责驱动怪物
    private bool isHostControlled = true;

    public string NetworkMonsterId
    {
        get { return networkMonsterId; }
    }

    public Entity entity;

    private CharacterController controller;
    private Animator animator;
    private AnimationEventRelay animationRelay;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        animationRelay = GetComponentInChildren<AnimationEventRelay>();
    }

    private void Start()
    {
        // 怪物必须有稳定网络ID
        // 建议在 Inspector 手动填写，例如 monster_01 / monster_02
        // 如果没填，临时使用 GameObject 名称，但同场景多个同名对象会有风险
        if (string.IsNullOrEmpty(networkMonsterId))
        {
            networkMonsterId = gameObject.name;
        }

        // 第一版怪物权威规则：房主驱动怪物，非房主只显示同步结果
        isHostControlled = NetworkBattleEntry.Instance == null || NetworkBattleEntry.Instance.IsHost;

        entity = GameEntry.Instance.world.CreateEntity();

        entity.Add(new TransformComponent
        {
            Position = transform.position,
            Rotation = transform.rotation,
            IsGrounded = false,
            CanMove = true,
            CanRotate = true
        });

        entity.Add(new VelocityComponent
        {
            MoveSpeed = moveSpeed,
            Gravity = gravity,
        });

        entity.Add(new AnimationComponent());

        entity.Add(new AnimationCommandComponent());
        entity.Add(new AnimationEventComponent());
        entity.Add(new AttackRuntimeComponent
        {
            IsAttacking = false,
            CurrentComboIndex = 0,
            ComboQueued = false,
            CanQueueNext = false,
            DamageAppliedThisAction = false,
            ComboResetTimer = 0f
        });

        entity.Add(new HitReactionStateComponent
        {
            IsInHitStun = false,
            HitStunTimer = 0f,
            KnockbackVelocity = Vector3.zero,
            KnockbackDamping = 0f
        });

        entity.Add(new AttackMotionComponent
        {
            Clips = attackMotionClips,
            IsActive = false,
            ActiveComboIndex = 0,
            Elapsed = 0f,
            LastCurveValue = 0f,
            WorldDirection = Vector3.forward,
            FrameVelocity = Vector3.zero
        });

        entity.Add(new HealthComponent
        {
            MaxHP = maxHP,
            CurrentHP = currentHP,
            IsDead = false,
        });

        entity.Add(new AttackComponent
        {
            Stages = attackStages,
            CooldownTimer = 0f,
            ComboResetTime = comboResetTime,
            InputBufferTime = inputBufferTime,
            MaxCombo = attackStages != null ? Mathf.Max(1, attackStages.Length) : 1
        });

        entity.Add(new CampComponent
        {
            CampId = campId
        });

        entity.Add(new DeathStateComponent
        {
            HasProcessedDeath = false,
            DestroyDelay = destroyDelay,
            DestroyTimer = 0f
        });

        entity.Add(new ViewComponent
        {
            Owner = gameObject,
            DestroyWithEntity = true
        });

        // 添加怪物网络身份
        entity.Add(new MonsterNetworkIdentityComponent
        {
            MonsterId = networkMonsterId,
            IsHostControlled = isHostControlled
        });

        // 只有房主负责怪物 AI / 移动意图 / 攻击请求 / 索敌
        // 非房主不添加这些组件，避免本地 MonsterAISystem / MovementSystem 抢夺网络同步结果
        if (isHostControlled)
        {
            entity.Add(new MoveIntentComponent
            {
                MoveInput = Vector2.zero
            });

            entity.Add(new AttackRequestComponent
            {
                IsRequesting = false
            });

            entity.Add(new TargetComponent
            {
                TargetEntityId = -1
            });

            entity.Add(new TargetingComponent
            {
                SearchRange = searchRange,
            });

            entity.Add(new MonsterAIComponent
            {
                State = MonsterAIState.Idle
            });
        }

        if (animationRelay != null)
        {
            animationRelay.Bind(entity);
        }

        // 注册登记到战斗网络入口，供房主同步 / 非房主接收
        if (NetworkBattleEntry.Instance != null)
        {
            NetworkBattleEntry.Instance.RegisterMonsterView(networkMonsterId, this);
        }
    }

    private void Update()
    {
        if(entity == null) return;

        var velocityComp = entity.Get<VelocityComponent>();
        var transComp = entity.Get<TransformComponent>();
        var animComp = entity.Get<AnimationComponent>();
        var healthComp = entity.Get<HealthComponent>();

        // 只有房主怪物才执行本地 CharacterController.Move
        // 非房主怪物的位置由 NetworkBattleEntry.ApplyMonsterBattleState 同步
        if (isHostControlled)
        {
            Vector3 finalMove = (velocityComp.PlanarVelocity + Vector3.up * velocityComp.VerticalVelocity) * Time.deltaTime;
            controller.Move(finalMove);
        }

        // 返回物理碰撞
        transComp.Position = transform.position;
        transComp.IsGrounded = controller.isGrounded;
        entity.Add(transComp);

        // 渲染旋转
        transform.rotation = transComp.Rotation;

        // 渲染动画
        animator.SetFloat("MoveSpeed", animComp.MoveSpeed, 0.1f, Time.deltaTime);
        animator.SetBool("HasMoveInput", animComp.HasMoveInput);
        animator.SetBool("IsGrounded", transComp.IsGrounded);
        animator.SetBool("IsDead", animComp.IsDead);

        if (entity.Has<AnimationCommandComponent>())
        {
            var cmd = entity.Get<AnimationCommandComponent>();

            if (cmd.TriggerAttack)
            {
                animator.SetBool("AttackEnd", false);
                animator.ResetTrigger("AttackTrigger");
                animator.SetInteger("AttackIndex", cmd.AttackIndex);
                animator.SetTrigger("AttackTrigger");

                cmd.TriggerAttack = false;
                entity.Add(cmd);
            }

            if (cmd.TriggerHit)
            {
                animator.ResetTrigger("AttackTrigger");
                animator.SetBool("AttackEnd", true);

                animator.ResetTrigger("HitTrigger");
                animator.SetTrigger("HitTrigger");

                cmd.TriggerHit = false;
                entity.Add(cmd);
            }

            animator.SetBool("AttackEnd", cmd.AttackEnd);

            if (cmd.TriggerDeath)
            {
                animator.ResetTrigger("AttackTrigger");
                animator.SetBool("AttackEnd", true);

                animator.ResetTrigger("DeathTrigger");
                animator.SetTrigger("DeathTrigger");

                cmd.TriggerDeath = false;
                entity.Add(cmd);
            }
        }

        if (monsterHealthBar != null)
        {
            monsterHealthBar.SetHealth(healthComp.CurrentHP, healthComp.MaxHP);
            monsterHealthBar.SetVisible(!healthComp.IsDead || healthComp.CurrentHP <= 0);
        }
    }

    private void OnDestroy()
    {
        // DestroySystem 销毁怪物 GameObject 后，清理 NetworkBattleEntry 中的怪物引用。
        // 否则房主可能继续尝试同步已经销毁的怪物，非房主也可能残留旧引用。
        if (NetworkBattleEntry.Instance != null)
        {
            NetworkBattleEntry.Instance.UnregisterMonsterView(networkMonsterId, this);
        }
    }

    private void OnDrawGizmos()
    {
        DrawAttackGizmos();
    }

    private void DrawAttackGizmos()
    {
        Gizmos.color = Color.red;

        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Vector3 forward = transform.forward;

        AttackStageDefinition stage = attackStages[0];

        float radius = stage.AttackRange;
        float halfAngle = stage.AttackAngle * 0.5f;

        int segments = 24;
        Vector3 prevPoint = origin + Quaternion.Euler(0f, -halfAngle, 0f) * forward * radius;

        for (int i = 1; i <= segments; i++)
        {
            float lerp = (float)i / segments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, lerp);
            Vector3 nextPoint = origin + Quaternion.Euler(0f, angle, 0f) * forward * radius;

            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }

        Vector3 leftEdge = origin + Quaternion.Euler(0f, -halfAngle, 0f) * forward * radius;
        Vector3 rightEdge = origin + Quaternion.Euler(0f, halfAngle, 0f) * forward * radius;

        Gizmos.DrawLine(origin, leftEdge);
        Gizmos.DrawLine(origin, rightEdge);

        // 中心朝向线
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + forward * radius);

        Gizmos.color = Color.cyan;
        DrawWireCircle(origin, stage.TargetSearchRange);
    }

    private void DrawWireCircle(Vector3 center, float radius, int segments = 40)
    {
        Vector3 prev = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float rad = t * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
