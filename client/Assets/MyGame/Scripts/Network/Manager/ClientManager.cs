using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System;
using SocketGameProtocol;

/// <summary>
/// “我负责把网络字节变成 MainPack，再把 MainPack 交出去。”
/// </summary>
public class ClientManager : BaseManager
{
    private Socket socket;
    private Message message;

    // 高频发送状态包时，统一加发送锁，避免并发 Send 造成异常
    private readonly object sendLock = new object();

    public ClientManager(GameFace face) : base(face) { }

    public override void OnInit()
    {
        base.OnInit();
        message = new Message();

        InitSocket();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        message = null;
        CloseSocket();
    }

    /// <summary>
    /// 初始化
    /// </summary>
    private void InitSocket()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            socket.Connect("127.0.0.1", 6666);
            StartReceive();
            face.ShowMessage("连接成功！");
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex);
            face.ShowMessage("连接失败！");
        }
    }

    /// <summary>
    /// 关闭socket
    /// </summary>
    private void CloseSocket()
    {
        try
        {
            // 先判断 socket 是否为空
            if (socket != null)
            {
                if (socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                }

                socket.Close();
                socket = null;
            }
        }
        catch
        {
            socket = null;
        }
    }

    private void StartReceive()
    {
        if (socket == null || !socket.Connected || message == null)
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
            Debug.LogWarning(ex);
            CloseSocket();
        }
    }

    private void ReceiveCallback(IAsyncResult iar)
    {
        try
        {
            if (socket == null || socket.Connected == false)
                return;

            int len = socket.EndReceive(iar);

            if (len == 0)
            {
                CloseSocket();
                return;
            }

            message.ReadBuffer(len, HandleResponse);

            StartReceive();
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex);
            CloseSocket();
        }
    }

    private void HandleResponse(MainPack pack)
    {
        face.HandleRespone(pack);
    }

    public void Send(MainPack pack)
    {
        if (pack == null)
            return;

        if (socket == null || !socket.Connected)
            return;

        try
        {
            byte[] data = Message.PackData(pack);

            lock (sendLock)
            {
                socket.Send(data);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex);
            CloseSocket();
        }
    }
}