using System;
using System.Linq;
using SocketGameProtocol;
using Google.Protobuf;

namespace SocketMultiplayerGameServer.Tool
{
    internal class Message
    {
        private byte[] buffer = new byte[8192];

        // buffer存入索引
        private int startindex;

        // 修改：单包最大长度，防止异常包头导致内存无限扩容
        private const int MaxPacketSize = 1024 * 1024;

        public byte[] Buffer
        {
            get { return buffer; }
        }

        public int StartIndex
        {
            get { return startindex; }
        }

        // 获得buffer剩余空间
        public int Remsize
        {
            get
            {
                return buffer.Length - startindex;
            }
        }

        // 消息解析 (把每次服务器接收到客户端的消息长度传入)
        public void ReadBuffer(int len, Action<MainPack> HandleRequest)
        {
            if (len <= 0)
                return;

            startindex += len;

            while (true)
            {
                // 修改：每轮循环都先判断是否已经拥有完整包头
                if (startindex < 4)
                    return;

                // 修改：每轮循环都重新读取当前buffer开头的包体长度
                // 原代码只在while外读取一次count，处理粘包时会用旧长度解析下一个包
                int count = BitConverter.ToInt32(buffer, 0);

                // 修改：非法包长保护
                if (count <= 0 || count > MaxPacketSize)
                {
                    Console.WriteLine("[Message] 非法包长，已清空缓冲区：" + count);
                    ResetBuffer();
                    return;
                }

                int fullPacketLength = count + 4;
                EnsureBufferSize(fullPacketLength);

                // 半包，继续等待下一次Receive
                if (startindex < fullPacketLength)
                    return;

                try
                {
                    // 进行解析消息，从包体开始解析
                    MainPack pack = (MainPack)MainPack.Descriptor.Parser.ParseFrom(buffer, 4, count);

                    if (HandleRequest != null)
                        HandleRequest(pack);
                }
                catch (Exception ex)
                {
                    // 修改：解析失败时清空缓冲，避免坏数据继续污染后续包
                    Console.WriteLine("[Message] protobuf解析失败，已清空缓冲区：" + ex.Message);
                    ResetBuffer();
                    return;
                }

                // 修改：处理完一个完整包后，把剩余粘包数据移动到buffer开头
                int remain = startindex - fullPacketLength;

                if (remain > 0)
                {
                    Array.Copy(buffer, fullPacketLength, buffer, 0, remain);
                }

                startindex = remain;

                // 继续while，尝试解析下一个粘在一起的包
            }
        }

        private void EnsureBufferSize(int needSize)
        {
            if (buffer.Length >= needSize)
                return;

            int newSize = buffer.Length;
            while (newSize < needSize)
            {
                newSize *= 2;
            }

            Array.Resize(ref buffer, newSize);
        }

        private void ResetBuffer()
        {
            startindex = 0;
            Array.Clear(buffer, 0, buffer.Length);
        }

        // 包装数据
        public static byte[] PackData(MainPack pack)
        {
            if (pack == null)
                return new byte[0];

            // 数据包体
            byte[] data = pack.ToByteArray();

            // 包头 整型转换成byte数组
            byte[] head = BitConverter.GetBytes(data.Length);

            // 连接包头 包体
            return head.Concat(data).ToArray();
        }
    }
}