using SocketGameProtocol;

public class BattleHitRequest : BattleNetRequestBase
{
    protected override ActionCode ListenActionCode
    {
        get { return ActionCode.BattleHit; }
    }
}