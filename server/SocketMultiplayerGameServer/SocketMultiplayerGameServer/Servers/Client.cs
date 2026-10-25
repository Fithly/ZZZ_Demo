using MySql.Data.MySqlClient;
using SocketGameProtocol;
using SocketMultiplayerGameServer.DAO;
using SocketMultiplayerGameServer.Tool;
using System;
using System.Net.Sockets;

namespace SocketMultiplayerGameServer.Servers
{
    class Client
    {
        private MySqlConnection sqlConnection;
        private string connstr = "database=zzz;data source=127.0.0.1;user=root;password=root;pooling=false;charset=utf8;port=3306";

        private Socket socket;
        private Message message;
        private UserData userData;

        private Server server;

        // 修改：发送锁，避免多线程广播 / 响应同时 Send
        private readonly object sendLock = new object();

        // 修改：关闭锁，防止 Close 被重复调用
        private readonly object closeLock = new object();
        private bool isClosed = false;

        public Room GetRoom
        {
            get; set;
        }

        public string UserName
        {
            get; set;
        }

        public UserData GetUserData
        {
            get { return userData; }
        }

        public MySqlConnection GetMySqlConnect
        {
            get { return sqlConnection; }
        }

        public Client(Socket socket, Server server)
        {
            try
            {
                sqlConnection = new MySqlConnection(connstr);
                sqlConnection.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine("连接数据库失败！" + ex.Message);
            }

            userData = new UserData();
            message = new Message();

            this.server = server;
            this.socket = socket;

            // 接受消息
            StartReceive();
        }

        // 开始接受数据
        void StartReceive()
        {
            if (isClosed || socket == null || message == null)
                return;

            try
            {
                socket.BeginReceive(
                    message.Buffer,
                    message.StartIndex,
                    message.Remsize,
                    SocketFlags.None,
                    ReceiveCallback,
                    null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("开始接收失败：" + ex.Message);
                Close();
            }
        }

        // 接受回调（处理解析消息）
        void ReceiveCallback(IAsyncResult iar)
        {
            try
            {
                if (isClosed || socket == null || socket.Connected == false)
                    return;

                int len = socket.EndReceive(iar);

                if (len == 0)
                {
                    Close();
                    return;
                }

                // 解析
                message.ReadBuffer(len, HandleRequest);

                // 再次接受
                StartReceive();
            }
            catch (Exception ex)
            {
                Console.WriteLine("客户端接收异常，准备断开：" + ex.Message);
                Close();
            }
        }

        public void Send(MainPack pack)
        {
            if (pack == null)
                return;

            if (isClosed || socket == null || socket.Connected == false)
                return;

            try
            {
                byte[] data = Message.PackData(pack);

                lock (sendLock)
                {
                    if (isClosed || socket == null || socket.Connected == false)
                        return;

                    socket.Send(data);
                }
            }
            catch (Exception ex)
            {
                // 修改：发送失败时直接关闭并清理房间，不把异常一直抛给Room广播
                Console.WriteLine("发送给客户端失败，已断开该客户端：" + ex.Message);
                Close();
            }
        }

        void HandleRequest(MainPack pack)
        {
            if (isClosed)
                return;

            server.HandleRequest(pack, this);
        }

        private void Close()
        {
            lock (closeLock)
            {
                if (isClosed)
                    return;

                isClosed = true;
            }

            try
            {
                // 防止直接关闭游戏 导致没有退出房间
                if (GetRoom != null)
                {
                    Room room = GetRoom;
                    GetRoom = null;

                    room.Exit(server, this);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("退出房间时异常：" + ex.Message);
            }

            try
            {
                if (server != null)
                    server.RemoveClient(this);
            }
            catch { }

            try
            {
                if (socket != null)
                {
                    try
                    {
                        if (socket.Connected)
                            socket.Shutdown(SocketShutdown.Both);
                    }
                    catch { }

                    socket.Close();
                    socket = null;
                }
            }
            catch { }

            try
            {
                if (sqlConnection != null)
                {
                    sqlConnection.Close();
                    sqlConnection = null;
                }
            }
            catch { }
        }
    }
}