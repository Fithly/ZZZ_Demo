using System.Collections.Generic;
using SocketGameProtocol;
using UnityEngine;

/// <summary>
/// 战斗网络入口
/// 
/// 职责：
/// 1. 管理本地玩家是谁
/// 2. 管理远端玩家引用
/// 3. 判断谁是房主
/// 4. 收发战斗同步消息
/// 
/// 当前阶段：
/// 先跑通玩家 Join / Input / State / Hit / Death 的网络入口
/// 不在这里做怪物权威同步
/// </summary>
public class NetworkBattleEntry : MonoBehaviour
{
    public static NetworkBattleEntry Instance;

    [Header("Sync")]
    [SerializeField] private float inputSyncInterval = 0.033f;
    [SerializeField] private float stateSyncInterval = 0.033f;

    [Header("Remote Smooth")]
    // 远端玩家速度达到该值后，才允许触发 Run_End
    // 你的玩家 moveSpeed 默认是 5f，所以这里先用 4.5f 对应 90% 奔跑速度
    [SerializeField] private float remoteRunEndMinSpeed = 4.5f;

    [Header("Network Hit Reaction")]
    [SerializeField] private float networkHitStunDuration = 0.25f;
    [SerializeField] private float networkKnockbackSpeed = 4.5f;
    [SerializeField] private float networkKnockbackDamping = 18f;

    [Header("Snapshot Interpolation")]
    [SerializeField] private float snapshotInterpolationBackTime = 0.1f;
    [SerializeField] private float maxSnapshotAge = 1.0f;

    // 网络事件去重，防止同一个 Hit / Death 被重复表现
    private readonly HashSet<string> processedNetworkEventSet = new HashSet<string>();
    private readonly Queue<string> processedNetworkEventQueue = new Queue<string>();
    private const int MaxProcessedNetworkEventCount = 256;

    // 每个网络对象一个快照缓冲
    // key 可以是玩家ID，也可以是怪物ID
    private readonly Dictionary<string, NetworkSnapshotBuffer> snapshotBufferDict = new Dictionary<string, NetworkSnapshotBuffer>();

    private readonly Dictionary<string, PlayerView> playerViewDict = new Dictionary<string, PlayerView>();      // 玩家
    private readonly Dictionary<string, int> playerSeatDict = new Dictionary<string, int>();                    // 玩家座位

    // 怪物网络表现列表
    // key = 怪物网络ID，value = 场景中的怪物表现对象
    private readonly Dictionary<string, MonsterView> monsterViewDict = new Dictionary<string, MonsterView>();

    // 网络调试数据
    public int SendBattleStateCount { get; private set; }
    public int ReceiveBattleStateCount { get; private set; }
    public int ReceiveBattleHitCount { get; private set; }
    public int ReceiveBattleDeathCount { get; private set; }
    public int SendBattleDestroyCount { get; private set; }
    public int ReceiveBattleDestroyCount { get; private set; }

    public int RemoteSnapshotObjectCount
    {
        get { return snapshotBufferDict.Count; }
    }

    public int RemotePlayerCount
    {
        get { return playerViewDict.Count; }
    }

    public int MonsterCount
    {
        get { return monsterViewDict.Count; }
    }

    public float SnapshotInterpolationBackTime
    {
        get { return snapshotInterpolationBackTime; }
    }

    // 远端玩家移动动画判定参数，保持和 LocomotionAnimationSystem 一致
    private const float RemoteMoveThreshold = 0.15f;                    // 输入死区
    private const float RemoteReverseDotThreshold = -0.6f;              // 方向点乘阈值
    private const float RemoteReverseInputGraceTime = 0.2f;             // 输入观察窗口

    // 远端表现状态，只负责插值和远端移动动画判断
    private readonly Dictionary<string, RemoteVisualState> remoteVisualStateDict = new Dictionary<string, RemoteVisualState>();


    private PlayerView localPlayerView;

    private string localPlayerName = string.Empty;
    private string localPlayerId = string.Empty;

    private bool isHost;           // 是房主
    private int tick;

    private float inputTimer;
    private float stateTimer;

    // 减少日志刷屏
    private bool hasLogReceiveState = false;

    private class RemoteVisualState
    {
        public PlayerView PlayerView;

        public Vector3 NetworkPlanarVelocity;

        // 远端 Run_End 判定不再只靠速度，而是缓存远端输入
        public Vector2 LastRemoteMoveInput;
        public Vector2 LastNonZeroMoveInput;

        public bool WasMovingLastFrame;
        public bool PendingRunEnd;
        public float PendingRunEndTimer;

        // 避免远端静止时每帧重复触发 Run_End
        public bool IsRunEndPlaying;

        //远端玩家也必须达到奔跑速度后，才允许触发 Run_End
        public bool HasReachedRunSpeed;

        // 远端状态阻断条件
        public bool IsDead;
        public bool IsAttacking;
        public bool IsInHitStun;
    }

    public bool IsHost
    {
        get { return isHost; }
    }

    public string LocalPlayerId
    {
        get { return localPlayerId; }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (GameFace.Instance != null)
        {
            localPlayerName = GameFace.Instance.LocalPlayerName;
            localPlayerId = localPlayerName;
        }

        // 进入 BattleScene 后自动挂载战斗网络 Request
        EnsureBattleRequests();

        // 自动挂载网络调试面板
        EnsureDebugOverlay();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // 自动补齐网络调试面板
    private void EnsureDebugOverlay()
    {
        if (GetComponent<NetworkDebugOverlay>() == null)
        {
            gameObject.AddComponent<NetworkDebugOverlay>();
        }
    }

    // 自动补齐所有战斗网络接收脚本
    // 这些脚本继承 BattleNetRequestBase，会复用 BaseRequest / RequestManager 的分发机制
    private void EnsureBattleRequests()
    {
        AddBattleRequest<BattleJoinRequest>();
        AddBattleRequest<BattleInputRequest>();
        AddBattleRequest<BattleStateRequest>();
        AddBattleRequest<BattleHitRequest>();
        AddBattleRequest<BattleDeathRequest>();
        AddBattleRequest<BattleSnapshotRequest>();
        AddBattleRequest<BattleDestroyRequest>();
    }

    private void AddBattleRequest<T>() where T : BattleNetRequestBase
    {
        if (GetComponent<T>() == null)
        {
            gameObject.AddComponent<T>();
        }
    }

    private void Update()
    {
        // 快照插值更新远端玩家和怪物
        UpdateSnapshotInterpolation(Time.deltaTime);

        // 远端玩家 Run_End 仍然使用输入缓冲判断
        UpdateRemoteRunEndStates(Time.deltaTime);

        if (localPlayerView == null)
            return;

        tick++;

        inputTimer += Time.deltaTime;
        stateTimer += Time.deltaTime;

        if (inputTimer >= inputSyncInterval)
        {
            inputTimer = 0f;
            SendLocalBattleInput();
        }

        if (stateTimer >= stateSyncInterval)
        {
            stateTimer = 0f;

            // 玩家状态和怪物状态合并成一个 BattleState 包发送。
            // 避免“房主玩家状态包”被后续“怪物状态包”顶掉。
            SendBattleStateFrame();
        }
    }

    // 统一更新玩家和怪物的快照插值表现
    private void UpdateSnapshotInterpolation(float deltaTime)
    {
        if (snapshotBufferDict.Count == 0)
            return;

        float renderTime = Time.time - snapshotInterpolationBackTime;

        foreach (var pair in snapshotBufferDict)
        {
            string netId = pair.Key;
            NetworkSnapshotBuffer buffer = pair.Value;

            if (buffer == null)
                continue;

            NetworkRemoteSnapshot sample;
            if (!buffer.TrySample(renderTime, maxSnapshotAge, out sample))
                continue;

            if (monsterViewDict.ContainsKey(netId))
            {
                ApplyMonsterSnapshot(sample);
            }
            else if (playerViewDict.ContainsKey(netId))
            {
                ApplyPlayerSnapshot(sample);
            }
        }
    }

    // 应用远端玩家快照
    private void ApplyPlayerSnapshot(NetworkRemoteSnapshot snapshot)
    {
        if (!playerViewDict.TryGetValue(snapshot.NetId, out PlayerView playerView))
            return;

        if (playerView == null)
            return;

        SetRemotePlayerTransform(playerView, snapshot.Position, snapshot.Rotation);
        SyncRemoteTransformComponent(playerView, snapshot.Position, snapshot.Rotation);

        if (playerView.entity == null)
            return;

        Entity entity = playerView.entity;

        if (entity.Has<VelocityComponent>())
        {
            var velocity = entity.Get<VelocityComponent>();
            velocity.PlanarVelocity = snapshot.PlanarVelocity;
            entity.Add(velocity);
        }

        if (entity.Has<HealthComponent>())
        {
            var health = entity.Get<HealthComponent>();
            health.CurrentHP = snapshot.CurrentHP;
            health.MaxHP = snapshot.MaxHP;
            health.IsDead = snapshot.IsDead;
            entity.Add(health);
        }

        bool oldIsAttacking = false;
        int oldComboIndex = 0;

        if (entity.Has<AttackRuntimeComponent>())
        {
            var runtime = entity.Get<AttackRuntimeComponent>();

            oldIsAttacking = runtime.IsAttacking;
            oldComboIndex = runtime.CurrentComboIndex;

            runtime.IsAttacking = snapshot.IsAttacking;
            runtime.CurrentComboIndex = snapshot.ComboIndex;
            entity.Add(runtime);
        }

        if (entity.Has<AnimationCommandComponent>())
        {
            var cmd = entity.Get<AnimationCommandComponent>();

            bool shouldTriggerAttack =
                snapshot.IsAttacking &&
                (!oldIsAttacking || oldComboIndex != snapshot.ComboIndex);

            if (shouldTriggerAttack)
            {
                cmd.AttackEnd = false;
                cmd.TriggerAttack = true;
                cmd.AttackIndex = Mathf.Max(1, snapshot.ComboIndex);
            }

            if (!snapshot.IsAttacking && oldIsAttacking)
            {
                cmd.TriggerAttack = false;
                cmd.AttackEnd = true;
            }

            if (snapshot.IsDead)
            {
                cmd.TriggerDeath = true;
            }

            entity.Add(cmd);
        }

        if (entity.Has<HitReactionStateComponent>())
        {
            var hitState = entity.Get<HitReactionStateComponent>();
            hitState.IsInHitStun = snapshot.IsInHitStun;
            entity.Add(hitState);
        }

        // 同步远端 Run_End 阻断状态
        RemoteVisualState visualState;
        if (remoteVisualStateDict.TryGetValue(snapshot.NetId, out visualState))
        {
            visualState.IsDead = snapshot.IsDead;
            visualState.IsAttacking = snapshot.IsAttacking;
            visualState.IsInHitStun = snapshot.IsInHitStun;

            // 保存网络速度，供远端 Run_End 判定
            visualState.NetworkPlanarVelocity = snapshot.PlanarVelocity;
        }
    }

    // 应用怪物快照
    private void ApplyMonsterSnapshot(NetworkRemoteSnapshot snapshot)
    {
        if (!monsterViewDict.TryGetValue(snapshot.NetId, out MonsterView monsterView))
            return;

        if (monsterView == null)
            return;

        CharacterController controller = monsterView.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        monsterView.transform.position = snapshot.Position;
        monsterView.transform.rotation = snapshot.Rotation;

        if (controller != null)
            controller.enabled = true;

        if (monsterView.entity == null)
            return;

        Entity entity = monsterView.entity;

        if (entity.Has<TransformComponent>())
        {
            var trans = entity.Get<TransformComponent>();
            trans.Position = snapshot.Position;
            trans.Rotation = snapshot.Rotation;
            entity.Add(trans);
        }

        if (entity.Has<VelocityComponent>())
        {
            var velocity = entity.Get<VelocityComponent>();
            velocity.PlanarVelocity = snapshot.PlanarVelocity;
            entity.Add(velocity);
        }

        if (entity.Has<HealthComponent>())
        {
            var health = entity.Get<HealthComponent>();
            health.CurrentHP = snapshot.CurrentHP;
            health.MaxHP = snapshot.MaxHP;
            health.IsDead = snapshot.IsDead;
            entity.Add(health);
        }

        bool oldIsAttacking = false;
        int oldComboIndex = 0;

        if (entity.Has<AttackRuntimeComponent>())
        {
            var runtime = entity.Get<AttackRuntimeComponent>();

            oldIsAttacking = runtime.IsAttacking;
            oldComboIndex = runtime.CurrentComboIndex;

            runtime.IsAttacking = snapshot.IsAttacking;
            runtime.CurrentComboIndex = snapshot.ComboIndex;
            entity.Add(runtime);
        }

        if (entity.Has<AnimationCommandComponent>())
        {
            var cmd = entity.Get<AnimationCommandComponent>();

            bool shouldTriggerAttack =
                snapshot.IsAttacking &&
                (!oldIsAttacking || oldComboIndex != snapshot.ComboIndex);

            if (shouldTriggerAttack)
            {
                cmd.AttackEnd = false;
                cmd.TriggerAttack = true;
                cmd.AttackIndex = Mathf.Max(1, snapshot.ComboIndex);
            }

            if (!snapshot.IsAttacking && oldIsAttacking)
            {
                cmd.TriggerAttack = false;
                cmd.AttackEnd = true;
            }

            if (snapshot.IsDead)
            {
                cmd.TriggerDeath = true;
            }

            entity.Add(cmd);
        }

        if (entity.Has<HitReactionStateComponent>())
        {
            var hitState = entity.Get<HitReactionStateComponent>();
            hitState.IsInHitStun = snapshot.IsInHitStun;
            entity.Add(hitState);
        }
    }

    /// <summary>
    /// 更新所有远端玩家的 Run_End 判定
    /// 
    /// 说明：
    /// 1. SnapshotInterpolation 只负责位置 / 旋转 / 状态表现
    /// 2. Run_End 仍然需要根据远端输入进行缓冲判断
    /// 3. 这里统一遍历 remoteVisualStateDict，复用 UpdateRemoteRunEndByInput
    /// </summary>
    private void UpdateRemoteRunEndStates(float deltaTime)
    {
        if (remoteVisualStateDict.Count == 0)
            return;

        foreach (var pair in remoteVisualStateDict)
        {
            string playerId = pair.Key;
            RemoteVisualState state = pair.Value;

            if (state == null)
                continue;

            // 修改：只处理远端玩家，不处理怪物
            // 怪物没有玩家输入，也不应该走玩家 Run_End 逻辑
            if (state.PlayerView == null)
                continue;

            UpdateRemoteRunEndByInput(playerId, state, deltaTime);
        }
    }

    /// <summary>
    /// 发送一帧战斗状态
    /// 
    /// 内容：
    /// 1. 本地玩家状态
    /// 2. 如果自己是房主，再附带所有怪物状态
    /// </summary>
    private void SendBattleStateFrame()
    {
        MainPack framePack = new MainPack
        {
            Requestcode = RequestCode.Game,
            Actioncode = ActionCode.BattleState
        };

        AppendLocalPlayerState(framePack);

        if (isHost)
        {
            AppendHostMonsterStates(framePack);
        }

        if (framePack.Battlestatepack.Count > 0)
        {
            Send(framePack);

            //网络调试计数
            SendBattleStateCount++;
        }
    }

    /// <summary>
    /// 把本地玩家状态追加到同一个 BattleState 帧包里
    /// </summary>
    private void AppendLocalPlayerState(MainPack framePack)
    {
        if (framePack == null)
            return;

        if (localPlayerView == null || localPlayerView.entity == null)
            return;

        Entity entity = localPlayerView.entity;

        if (!entity.Has<VelocityComponent>() ||
            !entity.Has<HealthComponent>())
            return;

        var velocity = entity.Get<VelocityComponent>();
        var health = entity.Get<HealthComponent>();

        Vector3 pos = localPlayerView.transform.position;
        Quaternion rot = localPlayerView.transform.rotation;
        float rotY = rot.eulerAngles.y;

        if (entity.Has<TransformComponent>())
        {
            var trans = entity.Get<TransformComponent>();
            trans.Position = pos;
            trans.Rotation = rot;
            entity.Add(trans);
        }

        bool isAttacking = false;
        int comboIndex = 0;

        if (entity.Has<AttackRuntimeComponent>())
        {
            var runtime = entity.Get<AttackRuntimeComponent>();
            isAttacking = runtime.IsAttacking;
            comboIndex = runtime.CurrentComboIndex;
        }

        bool isInHitStun = false;

        if (entity.Has<HitReactionStateComponent>())
        {
            var hitState = entity.Get<HitReactionStateComponent>();
            isInHitStun = hitState.IsInHitStun;
        }

        MainPack singlePack = BattleNetMessageFactory.CreateBattleState(
            localPlayerId,
            tick,
            pos,
            rotY,
            velocity.PlanarVelocity,
            health.CurrentHP,
            health.MaxHP,
            health.IsDead,
            isAttacking,
            comboIndex,
            isInHitStun);

        if (singlePack.Battlestatepack.Count > 0)
        {
            framePack.Battlestatepack.Add(singlePack.Battlestatepack[0]);
        }
    }

    /// <summary>
    /// 房主把所有怪物状态追加到同一个 BattleState 帧包里
    /// </summary>
    private void AppendHostMonsterStates(MainPack framePack)
    {
        if (framePack == null)
            return;

        if (!isHost)
            return;

        foreach (var pair in monsterViewDict)
        {
            string monsterId = pair.Key;
            MonsterView monsterView = pair.Value;

            if (monsterView == null || monsterView.entity == null)
                continue;

            Entity entity = monsterView.entity;

            if (!entity.Has<HealthComponent>() ||
                !entity.Has<VelocityComponent>())
                continue;

            var health = entity.Get<HealthComponent>();
            var velocity = entity.Get<VelocityComponent>();

            bool isAttacking = false;
            int comboIndex = 0;

            if (entity.Has<AttackRuntimeComponent>())
            {
                var runtime = entity.Get<AttackRuntimeComponent>();
                isAttacking = runtime.IsAttacking;
                comboIndex = runtime.CurrentComboIndex;
            }

            bool isInHitStun = false;

            if (entity.Has<HitReactionStateComponent>())
            {
                var hitState = entity.Get<HitReactionStateComponent>();
                isInHitStun = hitState.IsInHitStun;
            }

            Vector3 pos = monsterView.transform.position;
            float rotY = monsterView.transform.eulerAngles.y;

            MainPack singlePack = BattleNetMessageFactory.CreateBattleState(
                monsterId,
                tick,
                pos,
                rotY,
                velocity.PlanarVelocity,
                health.CurrentHP,
                health.MaxHP,
                health.IsDead,
                isAttacking,
                comboIndex,
                isInHitStun);

            if (singlePack.Battlestatepack.Count > 0)
            {
                framePack.Battlestatepack.Add(singlePack.Battlestatepack[0]);
            }
        }
    }

    /// <summary>
    /// 由 BattleSceneBootstrap 在生成玩家后调用
    /// </summary>
    public void RegisterPlayerView(string playerName, int seatIndex, bool isLocalPlayer, PlayerView playerView)
    {
        if (string.IsNullOrEmpty(playerName) || playerView == null)
            return;

        // 玩家列表
        if (!playerViewDict.ContainsKey(playerName))
            playerViewDict.Add(playerName, playerView);
        else
            playerViewDict[playerName] = playerView;

        // 玩家座位列表
        if (!playerSeatDict.ContainsKey(playerName))
            playerSeatDict.Add(playerName, seatIndex);
        else
            playerSeatDict[playerName] = seatIndex;

        if (isLocalPlayer)
        {
            localPlayerView = playerView;
            localPlayerName = playerName;
            localPlayerId = playerName;

            // 当前约定座位号 0 是房主
            isHost = seatIndex == 0;
        }
        else
        {
            // 登记远端玩家表现状态
            RegisterRemoteVisualState(playerName, playerView);
        }

        Debug.Log("[NetworkBattleEntry] 注册玩家：" + playerName + " 本地=" + isLocalPlayer + " 座位=" + seatIndex);
    }

    private void RegisterRemoteVisualState(string playerId, PlayerView playerView)
    {
        if (string.IsNullOrEmpty(playerId) || playerView == null)
            return;

        RemoteVisualState state;
        if (!remoteVisualStateDict.TryGetValue(playerId, out state))
        {
            state = new RemoteVisualState();
            remoteVisualStateDict.Add(playerId, state);
        }

        state.PlayerView = playerView;
        state.NetworkPlanarVelocity = Vector3.zero;
    }

    /// <summary>
    /// 本地玩家生成完成后，通知其他客户端：我已进入战斗
    /// </summary>
    public void SendLocalBattleJoin()
    {
        if (localPlayerView == null)
            return;

        // 本地玩家座位索引
        int seatIndex = 0;
        if (playerSeatDict.TryGetValue(localPlayerName, out int cachedSeat))
        {
            seatIndex = cachedSeat;
        }

        Vector3 pos = localPlayerView.transform.position;
        float rotY = localPlayerView.transform.eulerAngles.y;

        MainPack pack = BattleNetMessageFactory.CreateBattleJoin(
            localPlayerName,
            localPlayerId,
            seatIndex,
            isHost,
            1,
            100,
            pos,
            rotY);

        Send(pack);
    }

    /// <summary>
    /// 发送命中 / 扣血消息
    /// </summary>
    public void SendBattleHit(string attackerId, string targetId, int damage, int currentHP, int maxHP, Vector3 hitPos, Vector3 hitDir)
    {
        MainPack pack = BattleNetMessageFactory.CreateBattleHit(
            attackerId,
            targetId,
            damage,
            currentHP,
            maxHP,
            hitPos,
            hitDir,
            tick);

        Send(pack);
    }

    /// <summary>
    /// 发送死亡消息
    /// </summary>
    public void SendBattleDeath(string killerId, string targetId, Vector3 deathPos, float rotY)
    {
        MainPack pack = BattleNetMessageFactory.CreateBattleDeath(
            killerId,
            targetId,
            tick,
            deathPos,
            rotY);

        Send(pack);
    }

    /// <summary>
    /// 发送网络对象销毁消息
    /// </summary>
    public void SendBattleDestroy(string targetId, int entityType)
    {
        if (string.IsNullOrEmpty(targetId))
            return;

        MainPack pack = BattleNetMessageFactory.CreateBattleDestroy(
            targetId,
            tick,
            entityType);

        Send(pack);
        SendBattleDestroyCount++;
    }

    /// <summary>
    /// 发送玩家输入
    /// </summary>
    private void SendLocalBattleInput()
    {
        if (localPlayerView == null || localPlayerView.entity == null)
            return;

        Entity entity = localPlayerView.entity;
        if (!entity.Has<InputComponent>())
            return;

        var input = entity.Get<InputComponent>();

        MainPack pack = BattleNetMessageFactory.CreateBattleInput(
            localPlayerId,
            tick,
            input.MoveInput,
            input.LookInput,
            input.Attack,
            input.Skill,
            input.BackPackPanel,
            input.CharactorPanel);

        Send(pack);
    }

    private void Send(MainPack pack)
    {
        if (GameFace.Instance == null)
            return;

        GameFace.Instance.Send(pack);
    }

    // 处理服务端返回的战斗信息
    public void HandleBattleMessage(MainPack pack)
    {
        if (pack == null)
            return;

        switch (pack.Actioncode)
        {
            case ActionCode.BattleJoin:
                OnBattleJoin(pack);
                break;

            case ActionCode.BattleInput:
                OnBattleInput(pack);
                break;

            case ActionCode.BattleState:
                OnBattleState(pack);
                break;

            case ActionCode.BattleHit:
                OnBattleHit(pack);
                break;

            case ActionCode.BattleDeath:
                OnBattleDeath(pack);
                break;

            case ActionCode.BattleSnapshot:
                OnBattleSnapshot(pack);
                break;
            case ActionCode.BattleDestroy:
                OnBattleDestroy(pack);
                break;
        }
    }

    private void OnBattleJoin(MainPack pack)
    {
        BattleJoinPack joinPack = pack.Battlejoinpack;
        if (joinPack == null)
            return;

        if (joinPack.Playerid == localPlayerId)
            return;

        Debug.Log("[NetworkBattleEntry] 远端玩家进入战斗：" + joinPack.Playerid);
    }

    private void OnBattleInput(MainPack pack)
    {
        BattleInputPack inputPack = pack.Battleinputpack;
        if (inputPack == null)
            return;

        if (inputPack.Playerid == localPlayerId)
            return;

        // 所有客户端都缓存远端玩家输入，用来复刻本地 ReverseInputGraceTime 逻辑
        CacheRemoteInput(
            inputPack.Playerid,
            new Vector2(inputPack.MoveX, inputPack.MoveY));

        // 房主后续如果要做输入权威，也可以继续在这里处理
        if (!isHost)
            return;
    }

    // 缓存远端玩家输入
    private void CacheRemoteInput(string playerId, Vector2 moveInput)
    {
        if (string.IsNullOrEmpty(playerId))
            return;

        RemoteVisualState state;
        if (!remoteVisualStateDict.TryGetValue(playerId, out state))
        {
            state = new RemoteVisualState();
            remoteVisualStateDict.Add(playerId, state);
        }

        state.LastRemoteMoveInput = moveInput;

        // 到远端玩家的 BattleInput 后，不只存到 RemoteVisualState，还要写入远端玩家 ECS 的 MoveIntentComponent
        // BattleInput 是远端玩家的“输入意图”。
        // 本地玩家通过 InputSystem 写 MoveIntentComponent；
        // 远端玩家必须通过网络输入包写 MoveIntentComponent。
        // 这样 AnimationSystem 判断 HasMoveInput / CanEnterIdle 时，
        // 本地和远端才能走同一套语义。
        PlayerView playerView = null;
        if (!playerViewDict.TryGetValue(playerId, out playerView))
            return;

        if (playerView == null || playerView.entity == null)
            return;

        Entity entity = playerView.entity;

        if (entity.Has<MoveIntentComponent>())
        {
            var moveIntent = entity.Get<MoveIntentComponent>();
            moveIntent.MoveInput = moveInput;
            entity.Add(moveIntent);
        }
    }

    private void OnBattleState(MainPack pack)
    {
        if (pack.Battlestatepack == null || pack.Battlestatepack.Count == 0)
            return;

        ReceiveBattleStateCount++;

        if (!hasLogReceiveState)
        {
            hasLogReceiveState = true;
            Debug.Log("[NetworkBattleEntry] 已收到 BattleState，数量：" + pack.Battlestatepack.Count);
        }

        for (int i = 0; i < pack.Battlestatepack.Count; i++)
        {
            BattleStatePack statePack = pack.Battlestatepack[i];

            if (statePack == null)
                continue;

            if (statePack.Playerid == localPlayerId)
                continue;

            // 收到状态后不直接改位置，而是写入快照缓冲
            CacheRemoteSnapshot(statePack);
        }
    }

    // 把 BattleStatePack 转成快照，并写入对应对象的 SnapshotBuffer
    private void CacheRemoteSnapshot(BattleStatePack statePack)
    {
        if (statePack == null)
            return;

        if (string.IsNullOrEmpty(statePack.Playerid))
            return;

        NetworkRemoteSnapshot snapshot = new NetworkRemoteSnapshot();
        snapshot.NetId = statePack.Playerid;
        snapshot.Tick = statePack.Tick;
        snapshot.ReceiveTime = Time.time;

        snapshot.Position = BattleNetConvertUtility.ToVector3(statePack.Transform);
        snapshot.Rotation = BattleNetConvertUtility.ToQuaternion(statePack.Transform);
        snapshot.PlanarVelocity = new Vector3(statePack.PlanarVX, 0f, statePack.PlanarVZ);


        snapshot.CurrentHP = statePack.CurrentHP;
        snapshot.MaxHP = statePack.MaxHP;
        snapshot.IsDead = statePack.Isdead;
        snapshot.IsAttacking = statePack.Isattacking;
        snapshot.ComboIndex = statePack.ComboIndex;
        snapshot.IsInHitStun = statePack.IsInHitStun;

        NetworkSnapshotBuffer buffer;
        if (!snapshotBufferDict.TryGetValue(snapshot.NetId, out buffer))
        {
            buffer = new NetworkSnapshotBuffer();
            snapshotBufferDict.Add(snapshot.NetId, buffer);
        }

        buffer.AddSnapshot(snapshot);
    }

    // 远端玩家 Run_End 缓冲判定，逻辑对齐 LocomotionAnimationSystem
    private void UpdateRemoteRunEndByInput(string playerId, RemoteVisualState state, float deltaTime)
    {
        if (state == null || state.PlayerView == null || state.PlayerView.entity == null)
            return;

        Entity entity = state.PlayerView.entity;

        if (!entity.Has<AnimationCommandComponent>() ||
            !entity.Has<LocomotionStateComponent>())
            return;

        var cmd = entity.Get<AnimationCommandComponent>();
        var locomotion = entity.Get<LocomotionStateComponent>();

        // 修改：
        // 远端 Walk_End 播放结束后，也需要消费 AnimationEventComponent.RunEndFinished。
        // 本地由 LocomotionAnimationSystem 处理；
        // 远端没有 InputComponent，不会进入 LocomotionAnimationSystem，
        // 所以这里单独处理。
        if (entity.Has<AnimationEventComponent>())
        {
            var evt = entity.Get<AnimationEventComponent>();

            if (evt.RunEndFinished)
            {
                evt.RunEndFinished = false;

                locomotion.IsRunEndPlaying = false;
                locomotion.PendingRunEnd = false;
                locomotion.PendingRunEndTimer = 0f;
                locomotion.HasReachedRunSpeed = false;

                state.IsRunEndPlaying = false;
                state.PendingRunEnd = false;
                state.PendingRunEndTimer = 0f;
                state.HasReachedRunSpeed = false;

                entity.Add(evt);
            }
        }

        // 死亡 / 攻击 / 受击 时，不处理移动收招逻辑
        if (state.IsDead || state.IsAttacking || state.IsInHitStun)
        {
            locomotion.PendingRunEnd = false;
            locomotion.PendingRunEndTimer = 0f;
            locomotion.IsRunEndPlaying = false;
            locomotion.HasReachedRunSpeed = false;

            state.PendingRunEnd = false;
            state.PendingRunEndTimer = 0f;
            state.IsRunEndPlaying = false;
            state.HasReachedRunSpeed = false;

            bool blockedMoving = state.LastRemoteMoveInput.sqrMagnitude > RemoteMoveThreshold * RemoteMoveThreshold;
            locomotion.WasMovingLastFrame = blockedMoving;
            state.WasMovingLastFrame = blockedMoving;

            if (blockedMoving)
            {
                Vector2 dir = state.LastRemoteMoveInput.normalized;
                locomotion.LastNonZeroMoveInput = dir;
                state.LastNonZeroMoveInput = dir;
            }

            entity.Add(locomotion);
            return;
        }

        Vector2 currentInput = state.LastRemoteMoveInput;
        bool hasCurrentInput = currentInput.sqrMagnitude > RemoteMoveThreshold * RemoteMoveThreshold;
        Vector2 currentDir = hasCurrentInput ? currentInput.normalized : Vector2.zero;

        // 远端也必须达到奔跑速度后，才允许之后触发 Run_End
        float remoteSpeed = state.NetworkPlanarVelocity.magnitude;
        if (hasCurrentInput && remoteSpeed >= remoteRunEndMinSpeed)
        {
            locomotion.HasReachedRunSpeed = true;
            state.HasReachedRunSpeed = true;
        }

        Vector2 lastDir = locomotion.LastNonZeroMoveInput;
        bool hasLastDir = lastDir.sqrMagnitude > 0.0001f;

        bool reverseNow = false;
        if (hasCurrentInput && hasLastDir)
        {
            float dot = Vector2.Dot(lastDir, currentDir);
            reverseNow = dot <= RemoteReverseDotThreshold;
        }

        // 只要远端重新开始移动，就允许下次停步再触发 Run_End
        if (hasCurrentInput)
        {
            locomotion.IsRunEndPlaying = false;
            state.IsRunEndPlaying = false;
        }

        // 如果 Run_End 已经触发过，并且远端仍然没重新移动，不重复触发
        if (locomotion.IsRunEndPlaying && !hasCurrentInput)
        {
            entity.Add(locomotion);
            return;
        }

        // 1. 直接反向输入：取消 Run_End，不触发停步
        if (locomotion.WasMovingLastFrame && reverseNow)
        {
            locomotion.PendingRunEnd = false;
            locomotion.PendingRunEndTimer = 0f;
            locomotion.WasMovingLastFrame = true;
            locomotion.LastNonZeroMoveInput = currentDir;

            state.PendingRunEnd = false;
            state.PendingRunEndTimer = 0f;
            state.WasMovingLastFrame = true;
            state.LastNonZeroMoveInput = currentDir;

            entity.Add(locomotion);
            return;
        }

        // 2. 上一帧在移动，这一帧没输入：
        // 不立刻 Run_End，先开缓冲窗口
        if (locomotion.WasMovingLastFrame && !hasCurrentInput && !locomotion.PendingRunEnd)
        {
            if (locomotion.HasReachedRunSpeed)
            {
                locomotion.PendingRunEnd = true;
                locomotion.PendingRunEndTimer = RemoteReverseInputGraceTime;

                state.PendingRunEnd = true;
                state.PendingRunEndTimer = RemoteReverseInputGraceTime;
            }
            else
            {
                // 轻点移动键，不触发 Run_End
                locomotion.PendingRunEnd = false;
                locomotion.PendingRunEndTimer = 0f;
                locomotion.WasMovingLastFrame = false;

                state.PendingRunEnd = false;
                state.PendingRunEndTimer = 0f;
                state.WasMovingLastFrame = false;
            }
        }

        // 3. Pending 窗口里继续观察
        if (locomotion.PendingRunEnd)
        {
            locomotion.PendingRunEndTimer -= deltaTime;
            state.PendingRunEndTimer = locomotion.PendingRunEndTimer;

            // 如果窗口内来了反向输入，取消 Run_End
            if (hasCurrentInput && reverseNow)
            {
                locomotion.PendingRunEnd = false;
                locomotion.PendingRunEndTimer = 0f;
                locomotion.WasMovingLastFrame = true;
                locomotion.LastNonZeroMoveInput = currentDir;

                state.PendingRunEnd = false;
                state.PendingRunEndTimer = 0f;
                state.WasMovingLastFrame = true;
                state.LastNonZeroMoveInput = currentDir;

                entity.Add(locomotion);
                return;
            }

            // 4. 窗口结束仍没有输入，才触发 Run_End
            if (locomotion.PendingRunEndTimer <= 0f)
            {
                cmd.TriggerRunEnd = true;

                locomotion.PendingRunEnd = false;
                locomotion.PendingRunEndTimer = 0f;
                locomotion.IsRunEndPlaying = true;
                locomotion.WasMovingLastFrame = false;
                locomotion.HasReachedRunSpeed = false;

                state.PendingRunEnd = false;
                state.PendingRunEndTimer = 0f;
                state.IsRunEndPlaying = true;
                state.WasMovingLastFrame = false;
                state.HasReachedRunSpeed = false;

                entity.Add(cmd);
                entity.Add(locomotion);
                return;
            }
        }

        // 更新最近一次非零方向
        if (hasCurrentInput)
        {
            locomotion.LastNonZeroMoveInput = currentDir;
            state.LastNonZeroMoveInput = currentDir;
        }

        locomotion.WasMovingLastFrame = hasCurrentInput;
        state.WasMovingLastFrame = hasCurrentInput;

        entity.Add(cmd);
        entity.Add(locomotion);
    }

    // 统一处理远端玩家 Transform 校正
    private void SetRemotePlayerTransform(PlayerView playerView, Vector3 pos, Quaternion rot)
    {
        if (playerView == null)
            return;

        CharacterController controller = playerView.GetComponent<CharacterController>();

        if (controller != null)
            controller.enabled = false;

        playerView.transform.position = pos;
        playerView.transform.rotation = rot;

        if (controller != null)
            controller.enabled = true;
    }

    // 插值后同步 ECS TransformComponent，避免 ECS 和 View 漂移
    private void SyncRemoteTransformComponent(PlayerView playerView, Vector3 pos, Quaternion rot)
    {
        if (playerView == null || playerView.entity == null)
            return;

        Entity entity = playerView.entity;

        if (!entity.Has<TransformComponent>())
            return;

        var trans = entity.Get<TransformComponent>();
        trans.Position = pos;
        trans.Rotation = rot;
        entity.Add(trans);
    }


    private void OnBattleHit(MainPack pack)
    {
        BattleHitPack hitPack = pack.Battlehitpack;
        if (hitPack == null)
            return;

        // 房主是战斗权威端，伤害已经在本地 DamageSystem 执行过。
        // 如果服务端把 BattleHit 又广播回房主，房主必须忽略，否则会重复跳伤害数字。
        if (isHost)
            return;

        string eventKey = BuildBattleHitKey(hitPack);
        if (!TryMarkNetworkEventProcessed(eventKey))
            return;

        ReceiveBattleHitCount++;

        // 怪物受击同步
        if (monsterViewDict.TryGetValue(hitPack.Targetid, out MonsterView monsterView))
        {
            ApplyBattleHitToMonster(hitPack, monsterView);
            return;
        }

        // 玩家受击同步
        if (playerViewDict.TryGetValue(hitPack.Targetid, out PlayerView playerView))
        {
            ApplyBattleHitToPlayer(hitPack, playerView);
            return;
        }
    }

    private void OnBattleDeath(MainPack pack)
    {
        BattleDeathPack deathPack = pack.Battledeathpack;
        if (deathPack == null)
            return;

        // 修改：
        // 房主本地已经执行死亡逻辑，忽略广播回来的死亡事件
        if (isHost)
            return;

        string eventKey = BuildBattleDeathKey(deathPack);
        if (!TryMarkNetworkEventProcessed(eventKey))
            return;

        ReceiveBattleDeathCount++;

        // 怪物死亡同步
        if (monsterViewDict.TryGetValue(deathPack.Targetid, out MonsterView monsterView))
        {
            ApplyBattleDeathToMonster(deathPack, monsterView);
            return;
        }

        // 玩家死亡同步
        if (playerViewDict.TryGetValue(deathPack.Targetid, out PlayerView playerView))
        {
            ApplyBattleDeathToPlayer(deathPack, playerView);
            return;
        }
    }

    private void OnBattleDestroy(MainPack pack)
    {
        BattleDestroyPack destroyPack = pack.Battledestroypack;
        if (destroyPack == null)
            return;

        // 房主本地已经由 DestroySystem 销毁，不处理自己广播回来的销毁事件
        if (isHost)
            return;

        string eventKey = BuildBattleDestroyKey(destroyPack);
        if (!TryMarkNetworkEventProcessed(eventKey))
            return;

        ReceiveBattleDestroyCount++;

        // 当前第一版只处理怪物销毁
        if (destroyPack.EntityType == 2)
        {
            DestroyRemoteMonster(destroyPack.Targetid);
        }
    }

    #region 去重
    // 修改：构造 Hit 事件唯一Key
    private string BuildBattleHitKey(BattleHitPack hitPack)
    {
        return "Hit_" +
               hitPack.Attackerid + "_" +
               hitPack.Targetid + "_" +
               hitPack.Tick + "_" +
               hitPack.Damage + "_" +
               hitPack.CurrentHP;
    }

    // 修改：构造 Death 事件唯一Key
    private string BuildBattleDeathKey(BattleDeathPack deathPack)
    {
        return "Death_" +
               deathPack.Killerid + "_" +
               deathPack.Targetid + "_" +
               deathPack.Tick;
    }

    // 构造 Destroy 事件唯一Key
    private string BuildBattleDestroyKey(BattleDestroyPack destroyPack)
    {
        return "Destroy_" +
               destroyPack.Targetid + "_" +
               destroyPack.Tick + "_" +
               destroyPack.EntityType;
    }

    // 修改：记录已经处理过的网络事件
    private bool TryMarkNetworkEventProcessed(string eventKey)
    {
        if (string.IsNullOrEmpty(eventKey))
            return false;

        if (processedNetworkEventSet.Contains(eventKey))
            return false;

        processedNetworkEventSet.Add(eventKey);
        processedNetworkEventQueue.Enqueue(eventKey);

        while (processedNetworkEventQueue.Count > MaxProcessedNetworkEventCount)
        {
            string oldKey = processedNetworkEventQueue.Dequeue();
            processedNetworkEventSet.Remove(oldKey);
        }

        return true;
    }

    #endregion

    // 怪物受击同步
    private void ApplyBattleHitToMonster(BattleHitPack hitPack, MonsterView monsterView)
    {
        if (monsterView == null || monsterView.entity == null)
            return;

        Entity entity = monsterView.entity;

        if (entity.Has<HealthComponent>())
        {
            var health = entity.Get<HealthComponent>();
            health.CurrentHP = hitPack.CurrentHP;
            health.MaxHP = hitPack.MaxHP;
            health.IsDead = hitPack.CurrentHP <= 0;
            entity.Add(health);
        }

        if (entity.Has<AnimationCommandComponent>())
        {
            var cmd = entity.Get<AnimationCommandComponent>();
            cmd.TriggerHit = true;
            cmd.AttackEnd = true;
            entity.Add(cmd);
        }

        Vector3 hitPos = GetBattleHitPosition(hitPack, monsterView.transform.position);
        DamageNumberSpawner.Spawn(hitPack.Damage, hitPos);
    }

    // 玩家受击同步
    private void ApplyBattleHitToPlayer(BattleHitPack hitPack, PlayerView playerView)
    {
        if (playerView == null || playerView.entity == null)
            return;

        Entity entity = playerView.entity;

        if (entity.Has<HealthComponent>())
        {
            var health = entity.Get<HealthComponent>();
            health.CurrentHP = hitPack.CurrentHP;
            health.MaxHP = hitPack.MaxHP;
            health.IsDead = hitPack.CurrentHP <= 0;
            entity.Add(health);
        }

        Vector3 hitPos = GetBattleHitPosition(hitPack, playerView.transform.position);
        Vector3 hitDir = GetBattleHitDirection(hitPack);

        DamageNumberSpawner.Spawn(hitPack.Damage, hitPos);

        // 死亡时不追加受击硬直，死亡系统会处理死亡动画
        bool isDead = false;
        if (entity.Has<HealthComponent>())
        {
            var health = entity.Get<HealthComponent>();
            isDead = health.IsDead;
        }

        if (!isDead)
        {
            AppendHitReactionRequest(entity, new HitReactionRequestInfo
            {
                HitDirection = hitDir,
                StunDuration = networkHitStunDuration,
                KnockbackSpeed = networkKnockbackSpeed,
                KnockbackDamping = networkKnockbackDamping
            });
        }
        else
        {
            if (entity.Has<AnimationCommandComponent>())
            {
                var cmd = entity.Get<AnimationCommandComponent>();
                cmd.TriggerDeath = true;
                cmd.AttackEnd = true;
                entity.Add(cmd);
            }
        }
    }

    // 怪物死亡同步
    private void ApplyBattleDeathToMonster(BattleDeathPack deathPack, MonsterView monsterView)
    {
        if (monsterView == null || monsterView.entity == null)
            return;

        Vector3 deathPos = BattleNetConvertUtility.ToVector3(deathPack.DeathTransform);
        Quaternion deathRot = BattleNetConvertUtility.ToQuaternion(deathPack.DeathTransform);

        CharacterController controller = monsterView.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        monsterView.transform.position = deathPos;
        monsterView.transform.rotation = deathRot;

        if (controller != null)
            controller.enabled = true;

        Entity entity = monsterView.entity;

        if (entity.Has<HealthComponent>())
        {
            var health = entity.Get<HealthComponent>();
            health.CurrentHP = 0;
            health.IsDead = true;
            entity.Add(health);
        }

        if (entity.Has<AnimationCommandComponent>())
        {
            var cmd = entity.Get<AnimationCommandComponent>();
            cmd.TriggerDeath = true;
            cmd.AttackEnd = true;
            entity.Add(cmd);
        }
    }

    // 玩家死亡同步
    private void ApplyBattleDeathToPlayer(BattleDeathPack deathPack, PlayerView playerView)
    {
        if (playerView == null || playerView.entity == null)
            return;

        Vector3 deathPos = BattleNetConvertUtility.ToVector3(deathPack.DeathTransform);
        Quaternion deathRot = BattleNetConvertUtility.ToQuaternion(deathPack.DeathTransform);

        CharacterController controller = playerView.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        playerView.transform.position = deathPos;
        playerView.transform.rotation = deathRot;

        if (controller != null)
            controller.enabled = true;

        Entity entity = playerView.entity;

        if (entity.Has<HealthComponent>())
        {
            var health = entity.Get<HealthComponent>();
            health.CurrentHP = 0;
            health.IsDead = true;
            entity.Add(health);
        }

        if (entity.Has<AnimationCommandComponent>())
        {
            var cmd = entity.Get<AnimationCommandComponent>();
            cmd.TriggerDeath = true;
            cmd.AttackEnd = true;
            entity.Add(cmd);
        }
    }

    // 获得受击位置
    private Vector3 GetBattleHitPosition(BattleHitPack hitPack, Vector3 fallback)
    {
        if (hitPack.HitTransform != null)
            return BattleNetConvertUtility.ToVector3(hitPack.HitTransform);

        return fallback + Vector3.up * 1.5f;
    }

    // 获得受击方向
    private Vector3 GetBattleHitDirection(BattleHitPack hitPack)
    {
        Vector3 dir = new Vector3(hitPack.HitDirX, 0f, hitPack.HitDirZ);

        if (dir.sqrMagnitude <= 0.0001f)
            return Vector3.zero;

        return dir.normalized;
    }

    // 追加受击缓存
    private void AppendHitReactionRequest(Entity entity, HitReactionRequestInfo request)
    {
        HitReactionBufferComponent buffer;

        if (entity.Has<HitReactionBufferComponent>())
        {
            buffer = entity.Get<HitReactionBufferComponent>();
            if (buffer.Requests == null)
                buffer.Requests = new List<HitReactionRequestInfo>(4);
        }
        else
        {
            buffer = new HitReactionBufferComponent
            {
                Requests = new List<HitReactionRequestInfo>(4)
            };
        }

        buffer.Requests.Add(request);
        entity.Add(buffer);
    }

    private void OnBattleSnapshot(MainPack pack)
    {
        BattleSnapshotPack snapshot = pack.Battlesnapshotpack;
        if (snapshot == null)
            return;

        for (int i = 0; i < snapshot.States.Count; i++)
        {
            BattleStatePack statePack = snapshot.States[i];

            if (statePack == null)
                continue;

            if (statePack.Playerid == localPlayerId)
                continue;

            // BattleSnapshot 也进入快照缓冲。
            // 避免一部分状态走 SnapshotBuffer，一部分状态走旧的 ApplyBattleState，
            // 导致远端表现链路混乱。
            CacheRemoteSnapshot(statePack);
        }
    }


    // MonsterView 生成实体后调用，用于登记场景怪物
    public void RegisterMonsterView(string monsterId, MonsterView monsterView)
    {
        if (string.IsNullOrEmpty(monsterId) || monsterView == null)
            return;

        if (!monsterViewDict.ContainsKey(monsterId))
            monsterViewDict.Add(monsterId, monsterView);
        else
            monsterViewDict[monsterId] = monsterView;

        Debug.Log("[NetworkBattleEntry] 注册怪物：" + monsterId);
    }

    /// <summary>
    /// 怪物被 DestroySystem 销毁后，从网络表中移除
    /// 避免字典里残留已经销毁的 MonsterView。
    /// </summary>
    public void UnregisterMonsterView(string monsterId, MonsterView monsterView)
    {
        if (string.IsNullOrEmpty(monsterId))
            return;

        if (!monsterViewDict.TryGetValue(monsterId, out MonsterView cachedView))
            return;

        if (cachedView == monsterView)
        {
            monsterViewDict.Remove(monsterId);

            // 修改：怪物销毁时同步清理快照缓冲
            RemoveSnapshotBuffer(monsterId);

            Debug.Log("[NetworkBattleEntry] 注销怪物：" + monsterId);
        }
    }

    /// <summary>
    /// 销毁远端怪物
    /// </summary>
    private void DestroyRemoteMonster(string monsterId)
    {
        if (string.IsNullOrEmpty(monsterId))
            return;

        if (!monsterViewDict.TryGetValue(monsterId, out MonsterView monsterView))
            return;

        if (monsterView == null)
        {
            monsterViewDict.Remove(monsterId);
            RemoveSnapshotBuffer(monsterId);
            return;
        }

        Entity entity = monsterView.entity;

        // 修改：先移除网络缓存，避免下一帧继续采样已经销毁的怪物
        monsterViewDict.Remove(monsterId);
        RemoveSnapshotBuffer(monsterId);

        if (entity != null && GameEntry.Instance != null && GameEntry.Instance.world != null)
        {
            GameEntry.Instance.world.RemoveEntity(entity.ID);
        }

        Destroy(monsterView.gameObject);

        Debug.Log("[NetworkBattleEntry] 接收 BattleDestroy，销毁怪物：" + monsterId);
    }

    /// <summary>
    /// 移除指定对象的快照缓冲
    /// </summary>
    private void RemoveSnapshotBuffer(string netId)
    {
        if (string.IsNullOrEmpty(netId))
            return;

        if (snapshotBufferDict.ContainsKey(netId))
        {
            snapshotBufferDict.Remove(netId);
        }
    }
}