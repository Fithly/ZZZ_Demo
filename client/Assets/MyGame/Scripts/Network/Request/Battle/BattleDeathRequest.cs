using SocketGameProtocol;

public class BattleDeathRequest : BattleNetRequestBase
{
    protected override ActionCode ListenActionCode
    {
        get { return ActionCode.BattleDeath; }
    }
}