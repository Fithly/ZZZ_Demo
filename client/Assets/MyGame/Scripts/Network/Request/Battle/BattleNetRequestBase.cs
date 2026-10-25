using System.Collections.Generic;
using SocketGameProtocol;
using UnityEngine;

/// <summary>
/// 战斗消息接收请求基类
/// 
/// 职责：
/// 1. 复用现有 BaseRequest / RequestManager 分发机制
/// 2. OnResponse 只接收网络线程传来的包
/// 3. Update 再回到 Unity 主线程处理包
/// 
/// 注意：
/// 这个类是抽象类，不要挂到场景物体上。
/// 只挂 BattleJoinRequest / BattleInputRequest / BattleStateRequest 等子类。
/// </summary>
public abstract class BattleNetRequestBase : BaseRequest
{
    private readonly Queue<MainPack> packQueue = new Queue<MainPack>();

    // 一帧最多处理的战斗包数量
    // 防止后台暂停恢复后，大量旧包一帧全部执行导致卡顿
    private const int MaxProcessCountPerFrame = 128;

    protected abstract ActionCode ListenActionCode
    {
        get;
    }

    public override void Awake()
    {
        requestCode = RequestCode.Game;
        actionCode = ListenActionCode;
        base.Awake();
    }

    // 需要回到 Unity 主线程，才可以安全修改 Transform / Animator / UI
    public virtual void Update()
    {
        int processCount = 0;

        while (processCount < MaxProcessCountPerFrame)
        {
            MainPack pack = null;

            lock (packQueue)
            {
                if (packQueue.Count > 0)
                    pack = packQueue.Dequeue();
            }

            if (pack == null)
                break;

            ProcessBattlePack(pack);
            processCount++;
        }
    }

    private void ProcessBattlePack(MainPack pack)
    {
        if (pack == null)
            return;

        if (NetworkBattleEntry.Instance == null)
        {
            Debug.LogWarning("[BattleNetRequest] NetworkBattleEntry 不存在，无法处理战斗消息：" + actionCode);
            return;
        }

        // 处理服务端返回的战斗信息
        NetworkBattleEntry.Instance.HandleBattleMessage(pack);
    }

    public override void OnResponse(MainPack pack)
    {
        // OnResponse 来自 Socket 异步回调线程
        // 所以这里只入队，不直接操作 Unity 对象
        lock (packQueue)
        {
            // 房主会连续发送“玩家状态 + 怪物状态”，如果这里清队列，
            // 房主玩家状态会被后面的怪物状态顶掉，导致房间成员看不到房主操作。
            //
            // BattleSnapshot 是全量快照，可以清旧包。
            if (ListenActionCode == ActionCode.BattleSnapshot)
            {
                packQueue.Clear();
            }

            packQueue.Enqueue(pack);
        }
    }
}