
using SocketGameProtocol;

public class BattleJoinRequest : BattleNetRequestBase
{
    protected override ActionCode ListenActionCode
    {
        get { return ActionCode.BattleJoin; }
    }
}