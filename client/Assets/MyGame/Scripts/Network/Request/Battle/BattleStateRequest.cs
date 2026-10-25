using SocketGameProtocol;

public class BattleStateRequest : BattleNetRequestBase
{
    protected override ActionCode ListenActionCode
    {
        get { return ActionCode.BattleState; }
    }
}