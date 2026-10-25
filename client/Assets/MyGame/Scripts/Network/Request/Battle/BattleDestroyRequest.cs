using SocketGameProtocol;

public class BattleDestroyRequest : BattleNetRequestBase
{
    protected override ActionCode ListenActionCode
    {
        get { return ActionCode.BattleDestroy; }
    }
}