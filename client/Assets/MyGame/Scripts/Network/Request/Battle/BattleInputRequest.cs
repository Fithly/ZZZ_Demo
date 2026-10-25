using SocketGameProtocol;

public class BattleInputRequest : BattleNetRequestBase
{
    protected override ActionCode ListenActionCode
    {
        get { return ActionCode.BattleInput; }
    }
}