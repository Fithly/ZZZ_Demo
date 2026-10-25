using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using SocketMultiplayerGameServer.Controller;
using SocketMultiplayerGameServer.DAO;
using SocketGameProtocol;
using System.Security.Authentication;

namespace SocketMultiplayerGameServer.Servers
{
    class Server
    {
        private Socket socket;

        // 存储所有客户端 管理起来
        private List<Client> clientList = new List<Client>();
        // 管理所有房间
        private List<Room> roomList = new List<Room>();

        private ControllerManager controllerManager;

        // 构造器 指定端口绑定
        public Server(int port)
        {
            // 初始化
            controllerManager = new ControllerManager(this);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // 绑定
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            // 监听
            socket.Listen(0);
            // 应答
            StartAccept();
        }

        // 应答方法
        void StartAccept()
        {
            socket.BeginAccept(AcceptCallback, null);
        }

        // 应答回调
        void AcceptCallback(IAsyncResult iar)
        {
            // 处理客户端连接
            Socket client = socket.EndAccept(iar);
            clientList.Add(new Client(client, this));

            // 再次开始应答
            StartAccept();
        }

        // 处理接收消息
        public void HandleRequest(MainPack pack, Client client)
        {
            controllerManager.HandleRequest(pack, client);
        }

        public void RemoveClient(Client client)
        {
            clientList.Remove(client);
        }

        // 创建房间
        public MainPack CreateRoom(Client client, MainPack pack)
        {
            try
            {
                // 创建房间只取第一位房间信息
                Room room = new Room(client, pack.Roompack[0]);
                roomList.Add(room);
                foreach (PlayerPack p in room.GetPlayerInfo())
                {
                    pack.Playerpack.Add(p);
                }

                pack.Returncode = ReturnCode.Succeed;
            }
            catch
            {
                pack.Returncode = ReturnCode.Fail;
            }

            return pack;
        }

        // 查找房间
        public MainPack FindRoom()
        {
            MainPack pack = new MainPack();
            pack.Actioncode = ActionCode.FindRoom;

            try
            {
                // 列表里没有房间
                if (roomList.Count == 0)
                {
                    pack.Returncode = ReturnCode.NotRoom;
                    return pack;
                }

                // 将房间信息添加进房间列表 并返回成功
                foreach (Room room in roomList)
                {
                    pack.Roompack.Add(room.GetRoomInfo);
                }
                pack.Returncode = ReturnCode.Succeed;
            }
            catch
            {
                // 否则返回失败
                pack.Returncode = ReturnCode.Fail;
            }

            return pack;
        }

        public MainPack JoinRoom(Client client, MainPack pack)
        {
            foreach (Room r in roomList)
            {
                if (r.GetRoomInfo.Roomname.Equals(pack.Str))
                {
                    if (r.GetRoomInfo.Status == 0)
                    {
                        // 可以加入房间
                        r.Join(client);
                        pack.Roompack.Add(r.GetRoomInfo);
                        foreach (PlayerPack p in r.GetPlayerInfo())
                        {
                            pack.Playerpack.Add(p);
                        }

                        pack.Returncode = ReturnCode.Succeed;
                        return pack;
                    }
                    else
                    {
                        // 房间不可加入
                        pack.Returncode = ReturnCode.Fail;
                        return pack;
                    }
                }
            }
            // 没有此房间
            pack.Returncode = ReturnCode.NotRoom;
            return pack;
        }

        // 单个客户端和其他客户端接收的不同
        public MainPack ExitRoom(Client client, MainPack pack)
        {
            if (client.GetRoom == null)
            {
                pack.Returncode = ReturnCode.Fail;
                return pack;
            }

            client.GetRoom.Exit(this, client);

            pack.Returncode = ReturnCode.Succeed;
            return pack;
        }

        // 移除房间
        public void RemoveRoom(Room room)
        {
            roomList.Remove(room);
        }

        public void Chat(Client client, MainPack pack)
        {
            pack.Str = client.UserName + ":" + pack.Str;
            // 广播给其他客户端
            client.GetRoom.BroadCast(client, pack);
        }
    }
}
