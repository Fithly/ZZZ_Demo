using SocketGameProtocol;
using SocketMultiplayerGameServer.Servers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketMultiplayerGameServer.Controller
{
    internal class RoomController : BaseController
    {
        public RoomController()
        {
            requestCode = RequestCode.Room;
        }

        public MainPack CreateRoom(Server server, Client client, MainPack pack)
        {
            // 获得创建是否成功的信息
            return server.CreateRoom(client, pack);
        }

        public MainPack FindRoom(Server server, Client client, MainPack pack)
        {
            return server.FindRoom();
        }

        public MainPack JoinRoom(Server server, Client client, MainPack pack)
        {
            return server.JoinRoom(client, pack);
        }

        public MainPack Exit(Server server, Client client, MainPack pack)
        {
            return server.ExitRoom(client, pack);
        }

        public MainPack Chat(Server server, Client client, MainPack pack)
        {
            server.Chat(client, pack);

            // 转发给其他人 自身不需要响应
            return null;
        }

        public MainPack StartGame(Server server, Client client, MainPack pack)
        {
            pack.Returncode = client.GetRoom.StartGame(client);
            return pack;
        }
    }
}
