using System.Net.Sockets;
using System.Net;
using System;


namespace Server
{
    /// <summary>
    /// 登录认证通信器------用于收发消息
    /// </summary>
    public class Session
    {  
        private readonly int mID;
        private Socket mTcpSocket;

        public int Id { get { return mID; } }                       // 自身id
        public IPEndPoint tcpAdress, udpAdress;                     // TCP_Ip  UDP_Ip
        public Socket TcpSocket { get { return mTcpSocket; } }      // 客户端连接的 Socket
        public KCP mKCP = null;                                     // 可靠Udp连接

        private uint mNextUpdateTime = 0;
        private readonly DateTime utc_time = new DateTime(1970, 1, 1);
        private uint CurrentTime
        {
            get
            {
                return (uint)(Convert.ToInt64(DateTime.UtcNow.Subtract(utc_time).TotalMilliseconds) & 0xffffffff);
            }
        }

        public event OnReceiveHandler OnReceiveMessage_TCP;
        public event OnReceiveHandler OnReceiveMessage_UDP;
        public event OnReceiveHandler OnReceiveMessage_KCP;

        public bool IsConnected
        {
            get
            {
                if (mTcpSocket != null && mTcpSocket.Connected)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 创建一个Session认证
        /// </summary>
        /// <param name="id">id</param>
        /// <param name="sock">连接的Socket</param>
        /// <param name="serv">所属服务器</param>
        public Session(int id, Socket sock)
        {
            mID = id;
            mTcpSocket = sock;

            tcpAdress = (IPEndPoint)sock.RemoteEndPoint;

            if (KcpService.Instance != null)
            {
                mKCP = new KCP((uint)id, (byte[] data, int length) =>
                {
                    try
                    {
                        if (udpAdress != null && KcpService.Instance != null && KcpService.Instance.IsActive)
                        {
                            KcpService.Instance.Send(data, length, udpAdress);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Warning("Session 发送KCP消息回调：" + e.Message);
                    }
                });
                mKCP.NoDelay(1, 10, 2, 1);
            }
        }

        /// <summary>
        /// 发送TCP消息
        /// </summary>
        public void SendTcp(MessageBuffer message)
        {
            if (TcpService.Instance != null)
            {
                TcpService.Instance.Send(new MessageInfo(message, this));
            }
        }
        /// <summary>
        /// 发送UDP消息
        /// </summary>
        public void SendUdp(MessageBuffer message)
        {
            if (UdpService.Instance != null)
            {
                UdpService.Instance.Send(new MessageInfo(message, this));
            }
        }
        /// <summary>
        /// 发送KCP消息
        /// </summary>
        /// <param name="message"></param>
        public void SendKcp(MessageBuffer message)
        {
            if (mKCP != null)
            {
                lock (mKCP)
                {
                    mKCP.Send(message.DataBuffer);
                    mNextUpdateTime = 0;//可以马上更新
                }
            }
        }

        public void OnReceiveMessageTCP(MessageInfo messageInfo)
        {
            OnReceiveMessage_TCP?.Invoke(messageInfo);
        }
        public void OnReceiveMessageUDP(MessageInfo messageInfo)
        {
            OnReceiveMessage_UDP?.Invoke(messageInfo);
        }
        public void OnReceiveMessageKCP(MessageInfo messageInfo)
        {
            OnReceiveMessage_KCP?.Invoke(messageInfo);
        }
        public void OnReceiveMessageKCP(byte[] data, IPEndPoint ip)
        {
            if (mKCP == null)
            {
                return;
            }
            lock (mKCP)
            {
                mKCP.Input(data);

                for (int size = mKCP.PeekSize(); size > 0; size = mKCP.PeekSize())
                {
                    MessageBuffer message = new MessageBuffer(size);

                    if (mKCP.Recv(message.DataBuffer) > 0)
                    {
                        if (message.IsValid() && message.Extra() == Id)
                        {
                            if (udpAdress == null || udpAdress.Equals(ip) == false)
                            {
                                udpAdress = ip;
                            }
                            OnReceiveMessageKCP(new MessageInfo(message, this));
                        }
                    }
                }
            }

        }

        public void Disconnect()
        {
            if (mTcpSocket == null) return;

            OnReceiveMessage_TCP = null;
            OnReceiveMessage_UDP = null;
            OnReceiveMessage_KCP = null;

            mTcpSocket.Close();
            mTcpSocket = null;
            TcpService.Instance.mClientSessions.Remove(this);
        }

        /// <summary>
        /// 需要用KCP通讯的控制器进行驱动
        /// </summary>
        public void UpdateKcp()
        {
            if (mKCP == null) return;

            uint time = CurrentTime;
            if (time >= mNextUpdateTime)
            {
                mKCP.Update(time);
                mNextUpdateTime = mKCP.Check(time);
            }
        }
    }
}
