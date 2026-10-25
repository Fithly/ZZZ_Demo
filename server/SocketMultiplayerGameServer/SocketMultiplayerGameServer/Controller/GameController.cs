using SocketGameProtocol;
using SocketMultiplayerGameServer.Servers;

namespace SocketMultiplayerGameServer.Controller
{
    internal class GameController : BaseController
    {
        public GameController()
        {
            requestCode = RequestCode.Game;
        }

        /// <summary>
        /// 玩家进入战斗
        /// </summary>
        public MainPack BattleJoin(Server server, Client client, MainPack pack)
        {
            BroadcastInRoom(client, pack);
            return null;
        }

        /// <summary>
        /// 输入上报
        /// 当前阶段服务端只负责转发，不做权威校验
        /// </summary>
        public MainPack BattleInput(Server server, Client client, MainPack pack)
        {
            BroadcastInRoom(client, pack);
            return null;
        }

        /// <summary>
        /// 状态同步
        /// </summary>
        public MainPack BattleState(Server server, Client client, MainPack pack)
        {
            BroadcastInRoom(client, pack);
            return null;
        }

        /// <summary>
        /// 命中 / 扣血广播
        /// </summary>
        public MainPack BattleHit(Server server, Client client, MainPack pack)
        {
            BroadcastInRoom(client, pack);
            return null;
        }

        /// <summary>
        /// 死亡广播
        /// </summary>
        public MainPack BattleDeath(Server server, Client client, MainPack pack)
        {
            BroadcastInRoom(client, pack);
            return null;
        }

        /// <summary>
        /// 战斗快照
        /// </summary>
        public MainPack BattleSnapshot(Server server, Client client, MainPack pack)
        {
            BroadcastInRoom(client, pack);
            return null;
        }

        /// <summary>
        /// 网络对象销毁广播
        /// </summary>
        public MainPack BattleDestroy(Server server, Client client, MainPack pack)
        {
            BroadcastInRoom(client, pack);
            return null;
        }

        /// <summary>
        /// 兼容旧的 UpdateStatus
        /// </summary>
        public MainPack UpdateStatus(Server server, Client client, MainPack pack)
        {
            BroadcastInRoom(client, pack);
            return null;
        }

        private void BroadcastInRoom(Client client, MainPack pack)
        {
            if (client == null)
                return;

            if (client.GetRoom == null)
                return;

            // 战斗消息只转发给同房间其他客户端
            client.GetRoom.BroadCast(client, pack);
        }
    }
}