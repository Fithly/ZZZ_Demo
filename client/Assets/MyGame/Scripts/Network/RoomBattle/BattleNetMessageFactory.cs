using SocketGameProtocol;
using UnityEngine;

/// <summary>
/// 战斗网络消息创建工具
/// 
/// 职责：
/// 1. 统一创建 BattleJoin / BattleInput / BattleState / BattleHit / BattleDeath 消息
/// 2. 避免各个系统里重复手写 MainPack
/// 3. 只负责组包，不负责发送
/// 
/// 集中创建战斗网络包
/// </summary>
public static class BattleNetMessageFactory
{
    public static MainPack CreateBattleJoin(string playerName, string playerId, int seatIndex, bool isHost, int campId, int hp, Vector3 pos, float rotY)
    {
        MainPack pack = new MainPack();
        pack.Requestcode = RequestCode.Game;
        pack.Actioncode = ActionCode.BattleJoin;

        // 战斗加入包
        BattleJoinPack joinPack = new BattleJoinPack();
        joinPack.Playername = playerName;
        joinPack.Playerid = playerId;
        joinPack.Seatindex = seatIndex;
        joinPack.Ishost = isHost;
        joinPack.Campid = campId;
        joinPack.Hp = hp;
        joinPack.Transform = CreateTransformPack(pos, rotY);

        pack.Battlejoinpack = joinPack;
        return pack;
    }

    public static MainPack CreateBattleInput(string playerId, int tick, Vector2 moveInput, Vector2 lookInput,
                                                bool attack, bool skill, bool backpackPanel, bool charactorPanel)
    {
        MainPack pack = new MainPack();
        pack.Requestcode = RequestCode.Game;
        pack.Actioncode = ActionCode.BattleInput;

        // 输入上报包
        BattleInputPack inputPack = new BattleInputPack();
        inputPack.Playerid = playerId;
        inputPack.Tick = tick;
        inputPack.MoveX = moveInput.x;
        inputPack.MoveY = moveInput.y;
        inputPack.LookX = lookInput.x;
        inputPack.LookY = lookInput.y;
        inputPack.Attack = attack;
        inputPack.Skill = skill;
        inputPack.BackpackPanel = backpackPanel;
        inputPack.CharactorPanel = charactorPanel;

        pack.Battleinputpack = inputPack;
        return pack;
    }

    public static MainPack CreateBattleState(string playerId, int tick, Vector3 pos, float rotY, Vector3 planarVelocity,
                                                int currentHP, int maxHP, bool isDead, bool isAttacking, int comboIndex, bool isInHitStun)
    {
        MainPack pack = new MainPack();
        pack.Requestcode = RequestCode.Game;
        pack.Actioncode = ActionCode.BattleState;

        //状态同步包
        BattleStatePack statePack = new BattleStatePack();
        statePack.Playerid = playerId;
        statePack.Tick = tick;
        statePack.Transform = CreateTransformPack(pos, rotY);
        statePack.PlanarVX = planarVelocity.x;
        statePack.PlanarVZ = planarVelocity.z;
        statePack.CurrentHP = currentHP;
        statePack.MaxHP = maxHP;
        statePack.Isdead = isDead;
        statePack.Isattacking = isAttacking;
        statePack.ComboIndex = comboIndex;
        statePack.IsInHitStun = isInHitStun;

        pack.Battlestatepack.Add(statePack);
        return pack;
    }

    public static MainPack CreateBattleHit(string attackerId, string targetId, int damage, int currentHP, int maxHP,
                                            Vector3 hitPos, Vector3 hitDir, int tick)
    {
        MainPack pack = new MainPack();
        pack.Requestcode = RequestCode.Game;
        pack.Actioncode = ActionCode.BattleHit;

        // 命中 / 扣血广播包
        BattleHitPack hitPack = new BattleHitPack();
        hitPack.Attackerid = attackerId;
        hitPack.Targetid = targetId;
        hitPack.Damage = damage;
        hitPack.CurrentHP = currentHP;
        hitPack.MaxHP = maxHP;
        hitPack.HitTransform = CreateTransformPack(hitPos, 0f);
        hitPack.HitDirX = hitDir.x;
        hitPack.HitDirZ = hitDir.z;
        hitPack.Tick = tick;

        pack.Battlehitpack = hitPack;
        return pack;
    }

    public static MainPack CreateBattleDeath(string killerId, string targetId, int tick, Vector3 deathPos, float rotY)
    {
        MainPack pack = new MainPack();
        pack.Requestcode = RequestCode.Game;
        pack.Actioncode = ActionCode.BattleDeath;

        // 死亡广播包
        BattleDeathPack deathPack = new BattleDeathPack();
        deathPack.Killerid = killerId;
        deathPack.Targetid = targetId;
        deathPack.Tick = tick;
        deathPack.DeathTransform = CreateTransformPack(deathPos, rotY);

        pack.Battledeathpack = deathPack;
        return pack;
    }

    public static MainPack CreateBattleDestroy(string targetId, int tick, int entityType)
    {
        MainPack pack = new MainPack();
        pack.Requestcode = RequestCode.Game;
        pack.Actioncode = ActionCode.BattleDestroy;

        // 网络对象销毁包
        BattleDestroyPack destroyPack = new BattleDestroyPack();
        destroyPack.Targetid = targetId;
        destroyPack.Tick = tick;
        destroyPack.EntityType = entityType;

        pack.Battledestroypack = destroyPack;
        return pack;
    }

    private static BattleTransformPack CreateTransformPack(Vector3 pos, float rotY)
    {
        // 战斗专用 Transform 包
        BattleTransformPack transformPack = new BattleTransformPack();
        transformPack.PosX = pos.x;
        transformPack.PosY = pos.y;
        transformPack.PosZ = pos.z;
        transformPack.RotY = rotY;

        return transformPack;
    }
}