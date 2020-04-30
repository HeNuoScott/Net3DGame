using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System;

namespace Server
{
    /// <summary>
    /// UDP 服务器
    /// </summary>
    public class UdpService : UdpClient
    {
        private static UdpService instance = null;
        public static UdpService Instance { get { return instance; } }

        private readonly int mPort;
        private Queue<MessageInfo> mSendMessageQueue = new Queue<MessageInfo>();
        private Thread mReceiveThread, mSendThread;
        private bool mListening = false;
        public bool IsActive { get { return base.Client != null && base.Client.IsBound && mListening; } }

        public UdpService(int port) : base(port)
        {
            instance = this;
            mPort = port;
        }

        public bool Listen()
        {
            if (mListening) return true;

            mListening = true;

            mReceiveThread = new Thread(ReceiveThread);
            mSendThread = new Thread(SendThread);

            mReceiveThread.Start();
            mSendThread.Start();

            return true;
        }

        public new void Close()
        {
            base.Close();

            if (mSendThread != null)
            {
                mSendThread.Abort();
                mSendThread = null;
            }
            if (mReceiveThread != null)
            {
                mReceiveThread.Abort();

                mReceiveThread = null;
            }
        }

        public void Send(MessageInfo message)
        {
            if (message == null) return;

            lock (mSendMessageQueue)
            {
                mSendMessageQueue.Enqueue(message);
            }
        }

        private void SendThread()
        {
            while (IsActive)
            {
                lock (mSendMessageQueue)
                {
                    while (mSendMessageQueue.Count > 0)
                    {
                        MessageInfo message = mSendMessageQueue.Dequeue();

                        if (message == null) continue;

                        try
                        {
                            Send(message.Buffer.DataBuffer, message.Buffer.DataSize, message.Session.udpAdress);
                        }
                        catch (SocketException e)
                        {
                            Debug.Log(e.Message);
                        }
                        catch (Exception e)
                        {
                            Debug.Error("UDP发送消息错误：" + e.Message);
                            throw e;
                        }
                    }
                    mSendMessageQueue.Clear();
                }
                Thread.Sleep(1);
            }
        }

        private void ReceiveThread()
        {
            while (IsActive)
            {
                try
                {
                    IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = Receive(ref ip);

                    if (data.Length > 0)
                    {
                        Session c = TcpService.Instance.GetSession(ip);  // 根据IP查找Session

                        if (MessageBuffer.IsValid(data))
                        {
                            var buffer = new MessageBuffer(data);
                            if (c == null || c.Id != buffer.Extra())
                            {
                                // 在根据Ip查找到的Session对应不上消息发送方Socket
                                // 根据消息的ID查找Session
                                c = TcpService.Instance.GetSession(buffer.Extra());  
                            }

                            if (c != null)
                            {
                                if (c.udpAdress == null || c.udpAdress.Equals(ip) == false)
                                {
                                    c.udpAdress = ip; // 更新Session的IP地址
                                }

                                c.OnReceiveMessageUDP(buffer);
                            }
                        }
                    }

                    Thread.Sleep(1);

                }
                catch (SocketException e)
                {
                    Debug.Log("UDP接收消息：" + e.Message);
                    continue;
                }
                catch (Exception e)
                {
                    Debug.Error("UDP接收消息：" + e.Message);
                    throw e;
                }

            }
        }
    }
}
