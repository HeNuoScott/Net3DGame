using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Network
{
    /// <summary>
    /// 客户端网络服务
    /// </summary>
    public class ClientService : SingletonMono<ClientService>
    {
        public event OnConnectHandler On_TCP_Connect;
        public event OnDisconnectHandler On_TCP_Disconnect;
        public event OnMessageHandler On_TCP_Message;
        private string mIP;
        private int mTCPPort;
        private int mUDPPort;
        private Mode mode = Mode.LockStep;
        private Protocol protocol = Protocol.UDP;
        private TcpService mTcp;
        private UdpService mUdp;
        private KcpService mKcp;
        private Queue<MessageBuffer> mReceiveMessageQueue = new Queue<MessageBuffer>();
        private Queue<bool> mConnectResultQueue = new Queue<bool>();
        private int mAcceptSock = 0;
        public int AcceptSock { get { return mAcceptSock; } }
        public string token;
        public bool IsConnected
        {
            get
            {
                if (mTcp != null)
                {
                    return mTcp.IsConnected;
                }
                return false;
            }
        }

        private void OnTcpConnect()
        {
            lock (mConnectResultQueue)
            {
                mConnectResultQueue.Enqueue(true);
            }
        }
        private void OnTcpDisconnect()
        {
            lock (mConnectResultQueue)
            {
                mConnectResultQueue.Enqueue(false);
            }
        }
        private void OnReceive(MessageBuffer message)
        {
            if (message == null) return;
            lock (mReceiveMessageQueue)
            {
                mReceiveMessageQueue.Enqueue(message);
            }
        }
        private void Update()
        {
            lock (mReceiveMessageQueue)
            {
                while (mReceiveMessageQueue.Count > 0)
                {
                    MessageBuffer message = mReceiveMessageQueue.Dequeue();
                    if (message == null) continue;
                    On_TCP_Message?.Invoke(message);
                }
            }

            lock (mConnectResultQueue)
            {
                while (mConnectResultQueue.Count > 0)
                {
                    bool result = mConnectResultQueue.Dequeue();
                    if (result) On_TCP_Connect?.Invoke();
                    else On_TCP_Disconnect?.Invoke();
                }
            }
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="tcpPort"></param>
        /// <param name="udpPort"></param>
        public void Connect(string ip, int tcpPort, int udpPort,Mode initMode,Protocol initProtocol)
        {
            if (IsConnected) return;

            MessageBuffer.MESSAGE_MAX_VALUE = 1000;
            MessageBuffer.MESSAGE_MIN_VALUE = 0;

            mode = initMode;
            protocol = initProtocol;

            mTcp = new TcpService();

            mIP = ip;
            mTCPPort = tcpPort;
            mUDPPort = udpPort;

            mTcp.OnConnect += OnTcpConnect;
            mTcp.OnDisconnect += OnTcpDisconnect;
            mTcp.OnMessage += OnReceive;

            mTcp.Connect(mIP, mTCPPort);
        }

        /// <summary>
        /// 选择性连接UDP服务器或者KCP服务器
        /// </summary>
        /// <param name="sock"></param>
        /// <param name="protocol"></param>
        public void OnAccept(int sock, Protocol protocol)
        {
            mAcceptSock = sock;

            if (protocol == Protocol.UDP)
            {
                mUdp = new UdpService();
                mUdp.Connect(mIP, mUDPPort);
            }
            else
            {
                mKcp = new KcpService((uint)mAcceptSock);
                mKcp.Connect(mIP, mUDPPort);
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (mTcp != null)
            {
                mTcp.Close();
            }
            if (mUdp != null)
            {
                mUdp.Close();
            }
            if (mKcp != null)
            {
                mKcp.Close();
            }
        }

        public void SendUdp(MessageBuffer msg)
        {
            if (msg == null || mUdp == null) return;
            mUdp.Send(msg);
        }

        public void SendKcp(MessageBuffer msg)
        {
            if (msg == null || mKcp == null) return;
            mKcp.Send(msg);
        }

        public void SendTcp(MessageBuffer msg)
        {
            if (msg == null || mTcp == null) return;
            mTcp.Send(msg);
        }

        public static long Ping(IPEndPoint ip)
        {
            UdpClient client = new UdpClient();

            client.Connect(ip);

            Stopwatch watch = Stopwatch.StartNew();

            client.Send(new byte[] { byte.MaxValue }, 1);
            var data = client.Receive(ref ip);

            long millis = watch.Elapsed.Milliseconds;
            watch.Stop();

            client.Close();

            return millis;
        }
    }
}