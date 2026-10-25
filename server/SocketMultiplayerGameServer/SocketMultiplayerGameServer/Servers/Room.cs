using System;
using System.Collections.Generic;
using System.Threading;
using SocketGameProtocol;
using Google.Protobuf.Collections;

namespace SocketMultiplayerGameServer.Servers
{
    internal class Room
    {
        // 房间信息
        private RoomPack roomInfo;

        // 列表第一个人就是房主
        private List<Client> clientList = new List<Client>();

        // 修改：房间内客户端列表可能被不同线程访问
        private readonly object clientLock = new object();

        // 修改：防止房主重复点击开始游戏
        private bool isStarting = false;

        /// <summary>
        /// 返回房间信息
        /// </summary>
        public RoomPack GetRoomInfo
        {
            get
            {
                lock (clientLock)
                {
                    RefreshRoomStatusNoLock();
                    return roomInfo;
                }
            }
        }

        public Room(Client client, RoomPack pack)
        {
            roomInfo = pack;

            lock (clientLock)
            {
                clientList.Add(client);
                RefreshRoomStatusNoLock();
            }

            // 存储客户端加入的房间
            client.GetRoom = this;
        }

        // 修改：统一刷新房间状态
        private void RefreshRoomStatusNoLock()
        {
            roomInfo.Curnum = clientList.Count;

            // 游戏已经开始后，保持游戏中
            if (roomInfo.Status == 2)
                return;

            if (roomInfo.Maxnum > 0 && clientList.Count >= roomInfo.Maxnum)
                roomInfo.Status = 1;
            else
                roomInfo.Status = 0;
        }

        // 获得玩家信息
        public RepeatedField<PlayerPack> GetPlayerInfo()
        {
            RepeatedField<PlayerPack> pack = new RepeatedField<PlayerPack>();

            lock (clientLock)
            {
                foreach (Client c in clientList)
                {
                    PlayerPack player = new PlayerPack();
                    player.Playername = c.UserName;

                    // 修改：当前阶段 PlayerID 也用玩家名，和客户端 BattleState 的 Playerid 保持一致
                    player.PlayerID = c.UserName;
                    player.Hp = 100;

                    pack.Add(player);
                }
            }

            return pack;
        }

        // 修改：广播前创建快照，避免广播中断线修改 clientList
        private List<Client> GetClientSnapshot()
        {
            lock (clientLock)
            {
                return new List<Client>(clientList);
            }
        }

        // 参数：除了谁之外其他都发送, 消息
        public void BroadCast(Client client, MainPack pack)
        {
            List<Client> snapshot = GetClientSnapshot();

            foreach (Client c in snapshot)
            {
                if (client != null && ReferenceEquals(c, client))
                    continue;

                TrySend(c, pack);
            }
        }

        // 修改：广播给所有人，包括房主
        private void BroadCastAll(MainPack pack)
        {
            List<Client> snapshot = GetClientSnapshot();

            foreach (Client c in snapshot)
            {
                TrySend(c, pack);
            }
        }

        private void TrySend(Client client, MainPack pack)
        {
            if (client == null || pack == null)
                return;

            // Client.Send 内部会处理发送失败并关闭客户端。
            // 这里不再持续打印“房间广播失败”，避免刷屏掩盖真正错误。
            client.Send(pack);
        }

        public void Join(Client client)
        {
            lock (clientLock)
            {
                if (!clientList.Contains(client))
                    clientList.Add(client);

                client.GetRoom = this;
                RefreshRoomStatusNoLock();
            }

            MainPack pack = new MainPack();
            pack.Requestcode = RequestCode.Room;
            pack.Actioncode = ActionCode.PlayerList;

            foreach (PlayerPack player in GetPlayerInfo())
            {
                pack.Playerpack.Add(player);
            }

            // 加入者自己会收到 JoinRoom 响应，这里只广播给其他人
            BroadCast(client, pack);
        }

        public void Exit(Server server, Client client)
        {
            bool isHost = false;

            lock (clientLock)
            {
                if (clientList.Count > 0 && ReferenceEquals(client, clientList[0]))
                {
                    isHost = true;
                }
            }

            MainPack pack = new MainPack();
            pack.Requestcode = RequestCode.Room;

            if (isHost)
            {
                lock (clientLock)
                {
                    clientList.Remove(client);
                }

                client.GetRoom = null;
                pack.Actioncode = ActionCode.Exit;

                // 房主离开，通知其他人退出房间
                BroadCast(client, pack);

                server.RemoveRoom(this);
                return;
            }

            lock (clientLock)
            {
                clientList.Remove(client);
                client.GetRoom = null;
                RefreshRoomStatusNoLock();
            }

            pack.Actioncode = ActionCode.PlayerList;

            foreach (PlayerPack player in GetPlayerInfo())
            {
                pack.Playerpack.Add(player);
            }

            BroadCast(client, pack);
        }

        public ReturnCode StartGame(Client client)
        {
            lock (clientLock)
            {
                if (clientList.Count == 0)
                    return ReturnCode.Fail;

                // 不是房主
                if (!ReferenceEquals(client, clientList[0]))
                    return ReturnCode.Fail;

                if (isStarting)
                    return ReturnCode.Succeed;

                isStarting = true;

                // 房间状态改为游戏中
                roomInfo.Status = 2;
            }

            Thread startTime = new Thread(Timer);
            startTime.IsBackground = true;
            startTime.Start();

            return ReturnCode.Succeed;
        }

        private void Timer()
        {
            BroadCastAll(CreateChatPack("房主已启动游戏..."));
            Thread.Sleep(1000);

            for (int i = 5; i > 0; i--)
            {
                BroadCastAll(CreateChatPack(i.ToString()));
                Thread.Sleep(1000);
            }

            // 开始游戏
            BroadCastAll(CreateStartingPack());
        }

        private MainPack CreateChatPack(string str)
        {
            MainPack pack = new MainPack();
            pack.Requestcode = RequestCode.Room;
            pack.Actioncode = ActionCode.Chat;
            pack.Str = str;
            return pack;
        }

        private MainPack CreateStartingPack()
        {
            MainPack pack = new MainPack();
            pack.Requestcode = RequestCode.Room;
            pack.Actioncode = ActionCode.Starting;

            // Starting 包里带上最新房间玩家，客户端进入战斗前再次缓存
            foreach (PlayerPack player in GetPlayerInfo())
            {
                pack.Playerpack.Add(player);
            }

            return pack;
        }
    }
}