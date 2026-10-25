using SocketGameProtocol;

public class BattleSnapshotRequest : BattleNetRequestBase
{
    protected override ActionCode ListenActionCode
    {
        get { return ActionCode.BattleSnapshot; }
    }
}