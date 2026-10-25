using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerView : MonoBehaviour
{
    // === 面板调参区 ===
    [Header("Move")]
    [SerializeField] private float moveSpeed = 5f;      // 移动速度
    [SerializeField] private float gravity = -9.8f;     // 重力
    [SerializeField] private float acceleration = 25f;  // 移动加速度
    [SerializeField] private float deceleration = 35f;  // 移动减速度
    [SerializeField] private float stopSpeedEpsilon = 0.05f;  // 小于该速度时直接归零

    [Header("Health")]
    [SerializeField] private int maxHP = 100;           // 最大HP
    [SerializeField] private int currentHP = 100;       // 当前HP
    [SerializeField] private bool isDead = false;       // 是否死亡

    [Header("Attack")]
    [SerializeField] private AttackStageDefinition[] attackStages;
    [SerializeField] private float comboResetTime = 0.8f;          // 连招重置时间
    [SerializeField] private float inputBufferTime = 0.2f;         // 输入缓冲时间

    [Header("Attack Motion")]
    [SerializeField] private AttackMotionClip[] attackMotionClips;  // 连招配置

    [Header("Targeting")]
    [SerializeField] private float searchRange = 10f;  // 索敌范围

    [Header("Camp")]
    [SerializeField] private int campId = 1;  // 阵营

    [Header("Death")]
    [SerializeField] private float destroyDelay = 2.0f;  // 延迟销毁

    [Header("UI")]
    [SerializeField] public UIHealthBar playerHealthBar;  // 角色血条UI

    // === 本地 / 远端玩家标记 ===
    [Header("Network")]
    [SerializeField] private bool isLocalPlayer = true;
    [SerializeField] private string networkPlayerName = string.Empty;

    // === 内部引用 ===
    public Entity entity;

    private CharacterController controller;
    private Animator animator;
    private AnimationEventRelay animationRelay;

    // === 外部初始化接口 ===
    public void InitializeAsLocal(string playerName)
    {
        isLocalPlayer = true;
        networkPlayerName = playerName;
    }

    public void InitializeAsRemote(string playerName)
    {
        isLocalPlayer = false;
        networkPlayerName = playerName;
    }

    public bool IsLocalPlayer => isLocalPlayer;
    public string NetworkPlayerName => networkPlayerName;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        animationRelay = GetComponentInChildren<AnimationEventRelay>();
    }

    private void Start()
    {
        // 从GameEntry的World申请创建属于自己的实体
        entity = GameEntry.Instance.world.CreateEntity();

        // 添加组件并将面板的值 初始化
        entity.Add(new VelocityComponent
        {
            MoveSpeed = moveSpeed,
            Gravity = gravity,
            Acceleration = acceleration,
            Deceleration = deceleration,
            StopSpeedEpsilon = stopSpeedEpsilon
        });

        entity.Add(new TransformComponent
        {
            Position = transform.position,
            Rotation = transform.rotation,
            IsGrounded = false,
            CanMove = true,
            CanRotate = true
        });

        entity.Add(new MoveIntentComponent
        {
            MoveInput = Vector2.zero
        });

        // ===== 只有本地玩家才添加输入组件 =====
        if (isLocalPlayer)
        {
            entity.Add(new InputComponent());
        }

        // 本地和远端都添加 LocomotionStateComponent。
        // 本地玩家由 LocomotionAnimationSystem 更新它。
        // 远端玩家由 NetworkBattleEntry 根据 BattleInput 更新它。
        // 这样 AnimationSystem 可以统一读取 PendingRunEnd / IsRunEndPlaying，
        // 避免远端玩家在速度短暂归零时误回 Idle。
        entity.Add(new LocomotionStateComponent
        {
            LastNonZeroMoveInput = Vector2.zero,
            WasMovingLastFrame = false,
            IsRunEndPlaying = false,
            PendingRunEnd = false,
            PendingRunEndTimer = 0f,
            HasReachedRunSpeed = false
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
            IsDead = isDead
        });

        entity.Add(new AttackComponent
        {
            Stages = attackStages,
            CooldownTimer = 0f,
            ComboResetTime = comboResetTime,
            InputBufferTime = inputBufferTime,
            MaxCombo = attackStages != null ? Mathf.Max(1, attackStages.Length) : 1
        });

        entity.Add(new AttackRequestComponent
        {
            IsRequesting = false
        });

        entity.Add(new CampComponent
        {
            CampId = campId
        });

        entity.Add(new TargetComponent
        {
            TargetEntityId = -1
        });

        entity.Add(new TargetingComponent
        {
            SearchRange = searchRange
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

        // 新增网络身份组件
        // 当前阶段先用玩家名作为 PlayerId
        entity.Add(new NetworkIdentityComponent
        {
            PlayerId = networkPlayerName,
            PlayerName = networkPlayerName,
            IsLocalPlayer = isLocalPlayer,
            IsHost = NetworkBattleEntry.Instance != null && NetworkBattleEntry.Instance.IsHost && isLocalPlayer
        });

        if (animationRelay != null)
        {
            animationRelay.Bind(entity);  
        }
    }

    void Update()
    {
        if (entity == null) return;

        // === 实时调参同步  ===
        // 将面板上可能发生改变的值，实时覆写给 ECS 组件。
        // 这样你在运行游戏时修改 Inspector 里的 MoveSpeed，ECS逻辑立马生效！
        var velocityComp = entity.Get<VelocityComponent>();
        velocityComp.MoveSpeed = moveSpeed;
        velocityComp.Gravity = gravity;
        velocityComp.Acceleration = acceleration;
        velocityComp.Deceleration = deceleration;
        velocityComp.StopSpeedEpsilon = stopSpeedEpsilon;
        entity.Add(velocityComp);

        var attackComp = entity.Get<AttackComponent>();
        attackComp.Stages = attackStages;
        attackComp.ComboResetTime = comboResetTime;
        attackComp.InputBufferTime = inputBufferTime;
        attackComp.MaxCombo = attackStages != null ? Mathf.Max(1, attackStages.Length) : 1;
        entity.Add(attackComp);

        // ======================================

        var transComp = entity.Get<TransformComponent>();
        var animComp = entity.Get<AnimationComponent>();
        var healthComp = entity.Get<HealthComponent>();

        // 只有本地玩家才由 ECS Velocity + CharacterController 执行真实移动。
        // 远端玩家的位置由 NetworkBattleEntry 根据 BattleState 做插值同步，
        // 否则远端会同时受到“网络插值”和“本地 CharacterController.Move”两套位移影响，产生掉帧/抖动。
        if (isLocalPlayer)
        {
            // 1. 物理移动执行 (水平速度 + 纵向重力)
            Vector3 finalMove = (velocityComp.PlanarVelocity + Vector3.up * velocityComp.VerticalVelocity) * Time.deltaTime;
            controller.Move(finalMove);
        }

        // 2. 将物理执行后的结果反馈回 ECS (解决碰撞后的位移偏移)
        // 本地玩家：反馈 CharacterController.Move 后的位置
        // 远端玩家：反馈 NetworkBattleEntry 插值后的位置
        transComp.Position = transform.position;
        transComp.IsGrounded = controller.isGrounded;
        entity.Add(transComp);

        // 3. 渲染旋转
        transform.rotation = transComp.Rotation;

        // 4. 动画驱动
        animator.SetFloat("MoveSpeed", animComp.MoveSpeed, 0.1f, Time.deltaTime);
        animator.SetBool("HasMoveInput", animComp.HasMoveInput);

        // CanEnterIdle 专门控制 Walk_Start / Walk_Loop 是否允许直接回 Idle。
        // 不要再只用 HasMoveInput 或 MoveSpeed 控制回 Idle。
        animator.SetBool("CanEnterIdle", animComp.CanEnterIdle);

        animator.SetBool("IsGrounded", transComp.IsGrounded);
        animator.SetBool("IsDead", animComp.IsDead);

        // 去使用动画指令(一次性命令消费)
        if (entity.Has<AnimationCommandComponent>())
        {
            var cmd = entity.Get<AnimationCommandComponent>();

            if (cmd.TriggerRunEnd)
            {
                animator.ResetTrigger("RunTurnTrigger");
                animator.ResetTrigger("RunEndTrigger");
                animator.SetTrigger("RunEndTrigger");

                cmd.TriggerRunEnd = false;
                entity.Add(cmd);
            }

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
                //Debug.Log("我被打了");
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
                // 死亡时清理攻击触发，避免残留
                animator.ResetTrigger("AttackTrigger");
                animator.SetBool("AttackEnd", true);

                // 一次性触发死亡
                animator.ResetTrigger("DeathTrigger");
                animator.SetTrigger("DeathTrigger");

                cmd.TriggerDeath = false;
                entity.Add(cmd);
            }
        }

        // 更新血量
        if (playerHealthBar != null)
        {
            playerHealthBar.SetHealth(healthComp.CurrentHP, healthComp.MaxHP);
            playerHealthBar.SetVisible(true);
        }
    }

    private void OnDrawGizmos()
    {
        DrawAttackGizmos();
    }

    private void DrawAttackGizmos()
    {
        if (attackStages == null || attackStages.Length == 0)
            return;

        AttackStageDefinition stage = attackStages[0];

        Gizmos.color = Color.red;

        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Vector3 forward = transform.forward;

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