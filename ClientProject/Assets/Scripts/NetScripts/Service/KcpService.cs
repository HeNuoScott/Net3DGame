using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Network
{
    public class KcpService : UdpClient
    {
        private KCP mKCP;
        private uint mNextUpdateTime = 0;
        private IPEndPoint mServerAdress;
        private Thread mReceiveThread, mUpdateThread;
        private static readonly DateTime utc_time = new DateTime(1970, 1, 1);
        private static uint Current
        {
            get
            {
                return (uint)(Convert.ToInt64(DateTime.UtcNow.Subtract(utc_time).TotalMilliseconds) & 0xffffffff);
            }
        }

        public bool IsConnected { get { return Client != null && Client.Connected; } }

        public event OnConnectHandler OnConnect;
        public event OnMessageHandler OnMessage;
        public event OnDisconnectHandler OnDisconnet;

        /// <summary>
        /// 构建KCP服务器
        /// </summary>
        /// <param name="conv">可以看成客户端的id kcp 的(uint)id </param>
        public KcpService(uint conv)
        {
            mKCP = new KCP(conv, (byte[] data, int length)=>
            {
                if (IsConnected) Send(data, length);
            });
            mKCP.NoDelay(1, 10, 2, 1);
        }

        public new bool Connect(string ip, int port)
        {
            if (IsConnected)
            {
                return true;
            }

            mServerAdress = new IPEndPoint(IPAddress.Parse(ip), port);

            Connect(mServerAdress);

            if (IsConnected == false)
            {
                Close();
                return false;
            }

            mReceiveThread = new Thread(ReceiveThread);
            mUpdateThread = new Thread(SendThread);
            mReceiveThread.Start();
            mUpdateThread.Start();


            if (OnConnect != null)
            {
                OnConnect();
            }
            return true;
        }

        public new void Close()
        {
            base.Close();

            if (mReceiveThread != null)
            {
                mReceiveThread.Abort();
                mReceiveThread = null;
            }
            if (mUpdateThread != null)
            {
                mUpdateThread.Abort();
                mUpdateThread = null;
            }
            if (OnDisconnet != null)
            {
                OnDisconnet();
                OnDisconnet = null;
            }
        }

        public void Send(MessageBuffer message)
        {
            if (message == null || mKCP == null) return;

            lock (mKCP)
            {
                mKCP.Send(message.DataBuffer);
                mNextUpdateTime = 0;
            }
        }

        private void SendThread()
        {
            while (IsConnected)
            {
                try
                {
                    if (mKCP != null)
                    {
                        uint time = Current;
                        if (time >= mNextUpdateTime)
                        {
                            mKCP.Update(time);
                            mNextUpdateTime = mKCP.Check(time);
                        }
                    }
                }
                catch (Exception e)
                {
                    Close();
                    throw e;
                }

                Thread.Sleep(1);
            }
        }

        private void ReceiveThread()
        {
            while (IsConnected)
            {
                try
                {
                    IPEndPoint ip = mServerAdress;
                    byte[] data = Receive(ref ip);

                    if (data.Length > 0)
                    {
                        lock (mKCP)
                        {
                            mKCP.Input(data);

                            for (int size = mKCP.PeekSize(); size > 0; size = mKCP.PeekSize())
                            {
                                MessageBuffer message = new MessageBuffer(size);

                                if (mKCP.Recv(message.DataBuffer) > 0)
                                {
                                    if (message.IsValid())
                                    {
                                        OnMessage?.Invoke(message);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Close();
                    throw e;
                }

                Thread.Sleep(1);
            }
        }
    }
}
