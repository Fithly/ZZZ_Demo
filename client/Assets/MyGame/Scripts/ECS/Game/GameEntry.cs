using UnityEngine;

// 只负责创世（创建 World 和 Systems），它是一个纯粹的管理容器
public class GameEntry : MonoBehaviour
{
    public static GameEntry Instance { get; private set; }

    // ===== Unity对象引用 =====

    public Transform cameraTransform;    // 相机Transform

    // ===== ECS核心 =====

    public World world;
    private SystemScheduler scheduler;

    // ===== Systems =====

    private InputSystem inputSystem;
    private TargetSearchSystem targetSearchSystem;
    private MonsterAISystem monsterAISystem;
    private CombatInputSystem combatInputSystem;
    private AttackSystem attackSystem;
    private AttackAnimationEventSystem attackAnimationEventSystem;
    private AttackHitResolveSystem attackHitResolveSystem;
    private DamageSystem damageSystem;
    private DeathSystem deathSystem;
    private MovementSystem movementSystem;
    private AnimationSystem animationSystem;
    private DestroySystem destroySystem;
    private AttackMotionSystem attackMotionSystem;
    private HitReactionSystem hitReactionSystem;
    private LocomotionAnimationSystem locomotionAnimationSystem;

    private UIPanelInputSystem uiPanelInputSystem;

    private void Awake()
    {
        Instance = this;

        // 创建ECS世界
        world = new World();

        // 创建系统调度器
        scheduler = new SystemScheduler();

        // 初始化系统
        inputSystem = new InputSystem();

        targetSearchSystem = new TargetSearchSystem();
        monsterAISystem = new MonsterAISystem();
        combatInputSystem = new CombatInputSystem();

        attackSystem = new AttackSystem();
        attackAnimationEventSystem = new AttackAnimationEventSystem();
        attackHitResolveSystem = new AttackHitResolveSystem();
        attackMotionSystem = new AttackMotionSystem();

        hitReactionSystem = new HitReactionSystem();

        damageSystem = new DamageSystem();
        deathSystem = new DeathSystem();

        locomotionAnimationSystem = new LocomotionAnimationSystem();

        movementSystem = new MovementSystem();
        movementSystem.cameraTransform = cameraTransform;

        animationSystem = new AnimationSystem();
        destroySystem = new DestroySystem();

        uiPanelInputSystem = new UIPanelInputSystem();

        // 注册系统执行顺序
        scheduler.AddSystem(inputSystem);
        scheduler.AddSystem(uiPanelInputSystem);

        scheduler.AddSystem(targetSearchSystem);
        scheduler.AddSystem(monsterAISystem);
        scheduler.AddSystem(combatInputSystem);

        scheduler.AddSystem(attackSystem);
        scheduler.AddSystem(attackAnimationEventSystem);
        scheduler.AddSystem(attackMotionSystem);
        scheduler.AddSystem(attackHitResolveSystem);

        scheduler.AddSystem(damageSystem);
        scheduler.AddSystem(deathSystem);
        scheduler.AddSystem(hitReactionSystem);

        scheduler.AddSystem(locomotionAnimationSystem);
        scheduler.AddSystem(movementSystem);
        scheduler.AddSystem(animationSystem);
        scheduler.AddSystem(destroySystem);
    }

    void Update()
    {
        // 每帧执行所有System
        scheduler.Update(world, Time.deltaTime);
    }

    private void OnDestroy()
    {
        if(inputSystem != null)
            inputSystem.Destroy();
    }
}