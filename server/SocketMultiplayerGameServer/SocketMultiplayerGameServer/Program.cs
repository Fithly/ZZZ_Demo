using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SocketMultiplayerGameServer.Servers;

//服务端
namespace SocketMultiplayerGameServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server(6666);
            Console.Read();
        }
    }
}
