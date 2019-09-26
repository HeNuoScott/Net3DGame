namespace Net.Server
{
    using Net.Share;
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Tcp网络服务器 2019.9.9  
    /// 作者:龙兄
    /// QQ：1752062104
    /// </summary>
    public class TcpServer : NetServerBase
    {
        /// <summary>
        /// 启动服务器
        /// </summary>
        /// <param name="port">端口</param>
        /// <param name="workerThreads">处理线程数</param>
        public override void Start(int port = 666, int workerThreads = 5)
        {
            if (Server != null)//如果服务器套接字已创建
                throw new Exception("服务器已经运行，不可重新启动，请先关闭后在重启服务器");

            GUID = GetType().GUID;
            OnStartingHandle += OnStarting;
            OnStartupCompletedHandle += OnStartupCompleted;
            OnHasConnectHandle += OnHasConnect;
            OnRevdCustomBufferHandle += OnReceiveBuffer;
            OnRemoveClientEvent += OnRemoveClient;
            OnRevdBufferHandle += ReceiveBufferHandle;

            OnStartingHandle();
            logList.Add("服务器开始运行...");

            if (Instance == null)
            {
                Instance = this;
                NetConvert.AddNetworkBaseType();
            }

            Rpcs.AddRange(NetBehaviour.GetRpcs(this));
            Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//---TCP协议
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, port);//IP端口设置
            Server.Bind(ip);//绑定 IP端口
            Server.Listen(LineUp);

            new Thread(AcceptConnect) { IsBackground = true, Name = "AcceptConnect" }.Start();//创建接受连接线程
            new Thread(UnClientUpdate) { IsBackground = true, Name = "UnClientUpdate" }.Start();
            new Thread(TcpUpdate) { IsBackground = true, Name = "TcpUpdate" }.Start();

            new Thread(HeartUpdate) { IsBackground = true, Name = "HeartUpdate" }.Start();//创建心跳包线程
            new Thread(UnHeartUpdate) { IsBackground = true, Name = "UnHeartUpdate" }.Start();//创建未知客户端心跳包线程
            //new Thread(ReceiveHandle) { IsBackground = true, Name = "ReceiveHandle" }.Start();//创建处理线程
            new Thread(DebugThread) { IsBackground = true, Name = "DebugThread" }.Start();
            new Thread(DebugLogThread) { IsBackground = true, Name = "DebugLogThread" }.Start();

            while (workerThreads > 0)
            {
                workerThreads--;
                new Thread(TcpUpdate) { IsBackground = true, Name = "TcpUpdate " + workerThreads }.Start();
                //new Thread(ReceiveHandle) { IsBackground = true, Name = "ReceiveHandle " + workerThreads }.Start();
            }

            var scene = OnAddDefaultScene();
            DefaultScene = scene.Key;
            Scenes.TryAdd(scene.Key, scene.Value);
            OnStartupCompletedHandle();
            logList.Add("服务器启动成功!");
        }

        void AcceptConnect()
        {
            while (IsRunServer)
            {
                Thread.Sleep(1);
                try
                {
                    var client = Server.Accept();
                    var unClient = new NetPlayer(GUID, client) { LastTime = DateTime.Now.AddMinutes(5) };
                    OnHasConnectHandle?.Invoke(unClient);
                    logList.Add("有客户端连接:" + client.RemoteEndPoint.ToString());
                    UnClients.TryAdd(client.RemoteEndPoint, unClient);
                } catch {}
            }
        }

        void UnClientUpdate()
        {
            byte[] buffer = new byte[50000];
            while (IsRunServer)
            {
                Thread.Sleep(1);
                try
                {
                    Parallel.ForEach(UnClients.Values, (unClient) =>
                    {
                        if (unClient.Client == null | unClient.Stream == null)
                            return;
                        if (!unClient.Client.Connected)
                            return;
                        if (!unClient.Stream.DataAvailable)
                            return;
                        int count = unClient.Stream.Read(buffer, 0, buffer.Length);
                        if (ResolveUnBuffer(unClient, buffer, count) == -1)
                            UnClients.TryRemove(unClient.RemotePoint[GUID], out NetPlayer value);
                    });
                }
                catch (Exception e)
                {
                    logList.Add("未知客户端更新线程异常:" + e.Message);
                }
            }
        }

        void TcpUpdate()
        {
            byte[] buffer = new byte[50000];
            while (IsRunServer)
            {
                Thread.Sleep(1);
                try
                {
                    Parallel.ForEach(Clients.Values, (client) =>
                    {
                        if (client.Client == null | client.Stream == null)
                            return;
                        if (!client.Client.Connected)
                            return;
                        if (!client.Stream.DataAvailable)
                            return;
                        int count = client.Stream.Read(buffer, 0, buffer.Length);
                        receiveAmount++;
                        receiveCount += count;
                        //revdBufs.Enqueue(new ReceiveBuffer(buffer, count, client));
                        ResolveBuffer(client, buffer, count);//处理缓冲区数据
                    });
                }
                catch (Exception e)
                {
                    logList.Add("TCP主线程异常:" + e.Message);
                }
            }
        }

        //解析未知客户端数据缓冲区， 返回1：未知客户端表达没有通过， 返回-1：允许未知客户端进服务器，与其他客户端进行互动
        private int ResolveUnBuffer(NetPlayer unPlayer, byte[] buffer, int count)
        {
            int index = 0;
            while (index < count)
            {
                byte cmd = buffer[index];//[0] = 网络命令整形数据
                int size = BitConverter.ToUInt16(buffer, index + 1);// {[1],[2]} 网络数据长度大小
                switch (cmd)
                {
                    case NetCmd.SendHeartbeat:
                        Send(unPlayer, new byte[] { 6, 0, 0 }, 0, 3);//心跳回应 或 连接回应
                        goto Go;
                    case NetCmd.RevdHeartbeat:
                        unPlayer.heart = 0;
                        goto Go;
                    case NetCmd.QuitGame://退出程序指令
                        return -1;
                }
                NetPlayer unClient;
                if(cmd == NetCmd.EntityRpc)
                    unClient = unPlayer.OnUnClientRequest(cmd, buffer, index + 3, size);
                else
                    unClient = OnUnClientRequest(unPlayer, cmd, buffer, index + 3, size);
                if (unClient != null)//当有客户端连接时,如果允许用户添加此客户端
                {
                    if (!unClient.RemotePoint.ContainsKey(GUID))
                        unClient.RemotePoint.Add(GUID, unPlayer.Client.RemoteEndPoint);
                    if (!Scenes.ContainsKey(unClient.sceneID))//如果非法场景ID则使用默认场景ID
                        unClient.sceneID = DefaultScene;
                    unClient.RemotePoint[GUID] = unPlayer.Client.RemoteEndPoint;//防止旧的端口号
                    unClient.heart = 0;//心跳初始化
                    Clients.TryAdd(unPlayer.Client.RemoteEndPoint, unClient);//将网络玩家添加到集合中
                    if (unClient.playerID == string.Empty)
                        unClient.playerID = Share.Random.Range(1000000, 9999999).ToString();
                    Players.TryAdd(unClient.playerID, unClient);//将网络玩家添加到集合中
                    Scenes[DefaultScene].players.Add(unClient);//将网络玩家添加到主场景集合中
                    unClient.Scene = Scenes[DefaultScene];//赋值玩家所在的场景实体
                    unClient.AddRpc(unClient);
                    OnAddClientHandle?.Invoke(unClient);
                    return -1;
                }
                Go: index = index + size + 3;
            }
            return 1;
        }

        /// <summary>
        /// 解析网络数据包
        /// </summary>
        private void ResolveBuffer(NetPlayer client, byte[] buffer, int count)
        {
            int index = 0;
            while (index < count)
            {
                byte cmd = buffer[index];//[0] = 网络命令整形数据
                int size = BitConverter.ToUInt16(buffer, index + 1);// {[1],[2]} 网络数据长度大小
                OnRevdBufferHandle?.Invoke(client, cmd, buffer, index + 3, size);
                index = index + size + 3;
            }
        }
        
        /// <summary>
        /// 发送封装完成后的网络数据
        /// </summary>
        /// <param name="client">发送到的客户端</param>
        /// <param name="buffer">发送字节数组缓冲区</param>
        /// <param name="index">字节数组开始位置</param>
        /// <param name="count">字节数组长度</param>
        protected override void Send(NetPlayer client, byte[] buffer, int index, int count)
        {
            sendAmount++;
            sendCount += count;
            client.Client.Send(buffer, index, count, 0);
        }

        /// <summary>
        /// 未知客户端心跳处理
        /// </summary>
        protected override void UnHeartHandle()
        {
            foreach (var client in UnClients.Values)
            {
                try
                {
                    if (DateTime.Now > client.LastTime)
                        throw new Exception("未知客户端占用连接通道!");
                    Send(client, new byte[] { 5, 0, 0 }, 0, 3);
                }
                catch (Exception ex)
                {
                    UnClients.TryRemove(client.RemotePoint[GUID], out NetPlayer recClient);
                    recClient?.Dispose();
                    logList.Add("移除未知客户端:" + ex.Message);
                    recClient = null;
                }
            }
        }

        /// <summary>
        /// 客户端心跳处理
        /// </summary>
        protected override void HeartHandle()
        {
            foreach (var client in Clients.Values)
            {
                try
                {
                    Send(client, new byte[] { 5, 0, 0 }, 0, 3);
                }
                catch (Exception ex)
                {
                    if (Clients.TryRemove(client.RemotePoint[GUID], out NetPlayer recClient))
                        OnRemoveClientEvent?.Invoke(recClient);
                    recClient?.OnRemoveClient();
                    Players.TryRemove(client.playerID, out NetPlayer recClient1);
                    if (Scenes.ContainsKey(client.sceneID))
                        Scenes[client.sceneID].players.Remove(client);
                    recClient?.Dispose();
                    logList.Add("移除客户端: 玩家ID:" + client.playerID + " 玩家终端: " + client.RemotePoint[GUID].ToString() + " Code: " + ex.Message);
                    recClient = null;
                }
            }
        }

        /// <summary>
        /// 关闭服务器
        /// </summary>
        public override void Close()
        {
            base.Close();
            NetPlayer player;
            foreach (var unClient in UnClients)
            {
                UnClients.TryRemove(unClient.Key, out player);
                player.Dispose();
                player = null;
            }
            foreach (var client in Clients)
            {
                Clients.TryRemove(client.Key, out player);
                player.Dispose();
                player = null;
            }
        }
    }
}