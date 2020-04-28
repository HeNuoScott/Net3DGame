using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System;

namespace Server
{
    public class KcpService : UdpClient
    {
        private static KcpService instance = null;
        public static KcpService Instance { get { return instance; } }

        private readonly int mPort;
        private Thread mReceiveThread;
        private bool mListening = false;
        public bool IsActive { get { return base.Client != null && base.Client.IsBound && mListening; } }

        public KcpService(int port) : base(port)
        {
            instance = this;
            mPort = port;
        }

        public bool Listen()
        {
            if (mListening) return true;

            mListening = true;
            mReceiveThread = new Thread(ReceiveThread);
            mReceiveThread.Start();
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
        }

        private void ReceiveThread()
        {
            while (IsActive)
            {
                try
                {
                    IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = Receive(ref ip);

                    Session c = TcpService.Instance.GetSession(ip);

                    if (c == null)
                    {
                        Debug.Warning("KCPServer收到未知客户端的KCP消息！！！");
                    }
                    else
                    {
                        c.OnReceiveMessageKCP(data, ip);
                    }

                    Thread.Sleep(1);

                }
                catch (SocketException e)
                {
                    Debug.Log("KCP接收消息：" + e.Message);
                    continue;
                }
                catch (Exception e)
                {
                    Debug.Error("KCP接收消息：" + e.Message);
                    throw e;
                }
            }
        }
    }
}
