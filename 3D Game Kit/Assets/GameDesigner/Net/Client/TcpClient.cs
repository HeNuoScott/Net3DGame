namespace Net.Client
{
    using Net.Share;
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;

    /// <summary>
    /// Tcp网络客户端 2019.8.27
    /// </summary>
    [Serializable]
    public class TcpClient : NetClientBase
    {
        /// <summary>
        /// 构造可靠传输协议客户端
        /// </summary>
        public TcpClient() { }

        /// <summary>
        /// 构造可靠传输协议客户端
        /// </summary>
        /// <param name="useUnityThread">unity主线程进行更新</param>
        public TcpClient(bool useUnityThread)
        {
            UseUnityThread = useUnityThread;
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="host">IP地址</param>
        /// <param name="port">端口号</param>
        /// <param name="result">连接结果</param>
        public override void Connect(string host, int port, Action<bool> result)
        {
            openClient = true;
            if (Instance == null)
                Instance = this;
            if (Client == null) //如果套接字为空则说明没有连接上服务器
            {
                this.host = host;
                this.port = port;
                ConnectResult(host, port, result1 =>
                {
                    OnConnected(result1);
                    result(result1);
                });
            }
            else if (!Connected)
            {
                Client.Close();
                ConnectState = connectState = ConnectState.ConnectLost;
                LogHandle("服务器连接中断!");
                AbortedThread();
                result(false);
            }
            else
            {
                result(true);
            }
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        protected override void ConnectResult(string host, int port, Action<bool> result)
        {
            Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//创建套接字
            Client.BeginConnect(host, port, ac => {
                try
                {
                    Client.EndConnect(ac);
                    Connected = true;
                    StartupThread();
                    result(true);
                }
                catch
                {
                    Client?.Close();
                    Client = null;
                    result(false);
                }
            }, null);
        }
        
        /// <summary>
        /// 发包处理
        /// </summary>
        protected override void SendHandle(MemoryStream stream)
        {
            sendCount += (int)stream.Length;
            sendAmount++;
            Client.Send(stream.ToArray(), (int)stream.Length, 0);
        }

        /// <summary>
        /// 解析网络数据包
        /// </summary>
        protected override void ResolveBuffer(byte[] buffer, int index, int count)
        {
            while (index < count)
            {
                byte cmd = buffer[index];//[0] = 网络命令整形数据
                int size = BitConverter.ToUInt16(buffer, index + 1);// {[1],[2]} 网络数据长度大小
                RevdBufferHandle(cmd, buffer, index + 3, size);
                index = index + size + 3;
            }
        }

        /// <summary>
        /// 心跳处理
        /// </summary>
        protected override void HeartHandle()
        {
            while (openClient & currFrequency < 10)
            {
                Thread.Sleep(5000);//5秒发送一个心跳包
                try
                {
                    if (Connected & heart < 3)
                    {
                        Send(NetCmd.SendHeartbeat, new byte[] { 5 });
                        heart++;
                    }
                    else if (heart <= 3)//连接中断事件执行
                    {
                        ConnectState = connectState = ConnectState.ConnectLost;
                        heart = 4;
                        LogHandle("连接中断！");
                    }
                    else//尝试连接执行
                    {
                        Reconnection(10);
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// 关闭连接,释放线程以及所占资源
        /// </summary>
        public override void Close()
        {
            Connected = false;
            openClient = false;
            ConnectState = connectState = ConnectState.ConnectClosed;
            Thread.Sleep(1000);//给update线程一秒的时间处理关闭事件
            AbortedThread("HeartHandle");
            AbortedThread("UpdateHandle");
            Client?.Dispose();
            Client?.Close();
        }
    }
}