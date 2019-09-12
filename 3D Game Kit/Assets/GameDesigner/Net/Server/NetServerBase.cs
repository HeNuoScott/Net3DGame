namespace Net.Server
{
    using Net.Share;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    /// <summary>
    /// 网络服务器核心基类 2019.7.19  
    /// 作者:龙兄
    /// QQ：1752062104
    /// </summary>
    public abstract class NetServerBase
    {
        /// <summary>
        /// 服务器套接字
        /// </summary>
        public Socket Server { get; protected set; }
        /// <summary>
        /// 远程过程调用委托
        /// </summary>
        public List<NetDelegate> Rpcs { get; set; } = new List<NetDelegate>();
        /// <summary>
        /// 所有在线的客户端 与Players为互助字典
        /// </summary>
        public ConcurrentDictionary<EndPoint, NetPlayer> Clients { get; private set; } = new ConcurrentDictionary<EndPoint, NetPlayer>();
        /// <summary>
        /// 所有在线的客户端 与Clients为互助字典 所添加的键值为NetPlayer.playerID, 当未知客户端请求时返回的对象请把NetPlayer.playerID赋值好,方便后面用到
        /// </summary>
        public ConcurrentDictionary<string, NetPlayer> Players { get; private set; } = new ConcurrentDictionary<string, NetPlayer>();
        /// <summary>
        /// 未知客户端连接 或 刚连接服务器还未登录账号的IP 
        /// </summary>
        public ConcurrentDictionary<EndPoint, NetPlayer> UnClients { get; private set; } = new ConcurrentDictionary<EndPoint, NetPlayer>();
        /// <summary>
        /// 服务器场景，每个key都处于一个场景或房间，关卡，value是场景对象
        /// </summary>
        public ConcurrentDictionary<string, NetScene> Scenes { get; private set; } = new ConcurrentDictionary<string, NetScene>();
        /// <summary>
        /// 网络客户端实例
        /// </summary>
        public static NetServerBase Instance { get; protected set; }
        /// <summary>
        /// 数据缓冲区队列
        /// </summary>
        protected ConcurrentQueue<ReceiveBuffer> revdBufs = new ConcurrentQueue<ReceiveBuffer>();
        /// <summary>
        /// 待处理的接受队列长度
        /// </summary>
        public int RevdQueueCount { get { return revdBufs.Count; } }
        /// <summary>
        /// 服务器是否处于运行状态, 如果服务器套接字已经被释放则返回False, 否则返回True. 当调用Close方法后将改变状态
        /// </summary>
        public bool IsRunServer { get; private set; } = true;
        /// <summary>
        /// 获取或设置最大可排队人数， 如果未知客户端人数超出LineUp值将不处理超出排队的未知客户端数据请求 ， 默认排队1000人
        /// </summary>
        public int LineUp { get; set; } = 1000;
        /// <summary>
        /// 允许玩家在线人数最大值（玩家在线上限）
        /// </summary>
        public int OnlineLimit { get; set; } = 2000;
        /// <summary>
        /// 超出的排队人数，不处理的人数
        /// </summary>
        protected int exceededNumber = 0;
        /// <summary>
        /// 服务器爆满, 阻止连接人数 与OnlineLimit属性有关
        /// </summary>
        protected int blockConnection = 0;
        /// <summary>
        /// 服务器主大厅默认场景名称
        /// </summary>
        protected string DefaultScene { get; set; } = "MainScene";
        /// <summary>
        /// 服务器实例标识
        /// </summary>
        protected Guid GUID;
        /// <summary>
        /// 网络统计发送数据长度/秒
        /// </summary>
        protected int sendCount = 0;
        /// <summary>
        /// 网络统计发送次数/秒
        /// </summary>
        protected int sendAmount = 0;
        /// <summary>
        /// 网络统计解析次数/秒
        /// </summary>
        protected int resolveAmount = 0;
        /// <summary>
        /// 网络统计接收次数/秒
        /// </summary>
        protected int receiveAmount = 0;
        /// <summary>
        /// 网络统计接收长度/秒
        /// </summary>
        protected int receiveCount = 0;

        /// <summary>
        /// 开始运行服务器事件
        /// </summary>
        public Action OnStartingHandle;
        /// <summary>
        /// 服务器启动成功事件
        /// </summary>
        public Action OnStartupCompletedHandle;
        /// <summary>
        /// 当前有客户端连接触发事件
        /// </summary>
        public Action<NetPlayer> OnHasConnectHandle;
        /// <summary>
        /// 当添加客户端到所有在线的玩家集合中触发的事件
        /// </summary>
        public Action<NetPlayer> OnAddClientHandle;
        /// <summary>
        /// 当开始调用rpc函数事件, 多线程时5%不安全
        /// </summary>
        [Obsolete("不推荐使用！")]
        public Action<NetPlayer> OnInvokeRpcHandle;
        /// <summary>
        /// 当接收到网络数据处理事件
        /// </summary>
        public RevdBufferHandle OnRevdBufferHandle;
        /// <summary>
        /// 当接收自定义网络数据事件
        /// </summary>
        public RevdBufferHandle OnRevdCustomBufferHandle;
        /// <summary>
        /// 当移除客户端时触发事件
        /// </summary>
        public Action<NetPlayer> OnRemoveClientEvent;
        /// <summary>
        /// 当统计网络流量时触发
        /// </summary>
        public NetworkDataTraffic OnNetworkDataTraffic;
        /// <summary>
        /// 输出日志
        /// </summary>
        public Action<string> Log;
        /// <summary>
        /// 调式输出日志
        /// </summary>
        internal List<string> logList = new List<string>();

        /// <summary>
        /// 构造网络服务器函数
        /// </summary>
        public NetServerBase()
        {
            NetConvert.AddNetworkBaseType();
        }

        /// <summary>
        /// 获得所有在线的客户端对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> GetClients<T>() where T : class
        {
            List<T> clients = new List<T>();
            foreach (var v in Clients.Values)
                clients.Add(v as T);
            return clients;
        }

        /// <summary>
        /// 获得在线的客户端对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetPlayer<T>(string playerID) where T : NetPlayer
        {
            return Players[playerID] as T;
        }

        /// <summary>
        /// 获得所有在线的客户端对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> GetClients<T>(NetServerBase server) where T : class
        {
            List<T> clients = new List<T>();
            foreach (var v in server.Clients.Values)
                clients.Add(v as T);
            return clients;
        }

        /// <summary>
        /// 获得所有服务器场景
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> GetScenes<T>() where T : class
        {
            List<T> scenes = new List<T>();
            foreach (var v in Scenes.Values)
                scenes.Add(v as T);
            return scenes;
        }

        /// <summary>
        /// 当未知客户端发送数据请求，返回null，不添加到clients，返回对象，添加到clients中
        /// 客户端玩家的入口点，在这里可以控制客户端是否可以进入服务器与其他客户端进行网络交互
        /// 在这里可以用来判断客户端登录和注册等等进站许可
        /// </summary>
        /// <param name="unClient">客户端终端</param>
        /// <param name="buffer">缓冲区</param>
        /// <param name="cmd">命令</param>
        /// <param name="index">字节开始索引</param>
        /// <param name="count">字节长度</param>
        /// <returns></returns>
        protected virtual NetPlayer OnUnClientRequest(NetPlayer unClient, byte cmd, byte[] buffer, int index, int count)
        {
            return unClient;
        }

        /// <summary>
        /// 当开始启动服务器
        /// </summary>
        protected virtual void OnStarting() { }

        /// <summary>
        /// 当服务器启动完毕
        /// </summary>
        protected virtual void OnStartupCompleted() { }

        /// <summary>
        /// 当添加默认网络场景，服务器初始化后会默认创建一个主场景，供所有玩家刚登陆成功分配的临时场景，默认初始化场景人数为1000人
        /// </summary>
        /// <returns>返回值string：网络玩家所在的场景名称 , 返回值NetScene：网络玩家的场景对象</returns>
        protected virtual KeyValuePair<string, NetScene> OnAddDefaultScene()
        {
            return new KeyValuePair<string, NetScene>(DefaultScene, new NetScene(1000));
        }

        /// <summary>
        /// 当有客户端连接
        /// </summary>
        /// <param name="client">客户端套接字</param>
        protected virtual void OnHasConnect(NetPlayer client) { }

        /// <summary>
        /// 当服务器判定客户端为断线或连接异常时，移除客户端时调用
        /// </summary>
        /// <param name="client">要移除的客户端</param>
        protected virtual void OnRemoveClient(NetPlayer client) { }

        /// <summary>
        /// 当开始调用服务器RPCFun函数 或 开始调用自定义网络命令时 可设置请求客户端的client为全局字段，可方便在服务器RPCFun函数内引用!!!
        /// 在多线程时有1%不安全，当出现client赋值错误诡异时，可在网络函数加上 RPCFunc(NetCmd.SafeCall) 命令
        /// </summary>
        /// <param name="client">发送请求数据的客户端</param>
        protected virtual void OnInvokeRpc(NetPlayer client) { }

        /// <summary>
        /// 当接收到客户端自定义数据请求,在这里可以使用你自己的网络命令，系列化方式等进行解析网络数据。（你可以在这里使用ProtoBuf或Json来解析网络数据）
        /// </summary>
        /// <param name="client">当前客户端</param>
        /// <param name="cmd">网络命令</param>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="index">数据的开始索引</param>
        /// <param name="count">数据长度</param>
        protected virtual void OnReceiveBuffer(NetPlayer client, byte cmd, byte[] buffer, int index, int count) { }

        /// <summary>
        /// 运行服务器
        /// </summary>
        /// <param name="port">服务器端口号</param>
        /// <param name="workerThreads">处理线程数</param>
        public void Run(int port = 666, int workerThreads = 5) => Start(port, workerThreads);

        /// <summary>
        /// 启动服务器
        /// </summary>
        /// <param name="port">端口</param>
        /// <param name="workerThreads">处理线程数</param>
        public virtual void Start(int port = 666, int workerThreads = 5)
        {
            if (Server != null)//如果服务器套接字已创建
                throw new Exception("服务器已经运行，不可重新启动，请先关闭后在重启服务器");

            GUID = GetType().GUID;
            OnStartingHandle += OnStarting;
            OnStartupCompletedHandle += OnStartupCompleted;
            OnHasConnectHandle += OnHasConnect;
            //OnInvokeRpcHandle += OnInvokeRpc;
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
            Server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);//---UDP协议
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, port);//IP端口设置
            Server.Bind(ip);//绑定UDP IP端口

            uint IOC_IN = 0x80000000;
            uint IOC_VENDOR = 0x18000000;
            uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
            Server.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);//udp远程关闭现有连接方案

            new Thread(UdpUpdate) { IsBackground = true , Name = "UdpUpdate" }.Start();//创建UDP每一帧线程
            new Thread(HeartUpdate) { IsBackground = true, Name = "HeartUpdate" }.Start();//创建心跳包线程
            new Thread(UnHeartUpdate) { IsBackground = true, Name = "UnHeartUpdate" }.Start();//创建未知客户端心跳包线程
            new Thread(ReceiveHandle) { IsBackground = true, Name = "ReceiveHandle" }.Start();//创建处理线程
            new Thread(DebugThread) { IsBackground = true, Name = "DebugThread" }.Start();
            new Thread(DebugLogThread) { IsBackground = true, Name = "DebugLogThread" }.Start();

            while (workerThreads > 0)
            {
                workerThreads--;
                new Thread(UdpUpdate) { IsBackground = true, Name = "UdpUpdate " + workerThreads }.Start();//创建UDP每一帧线程
                new Thread(ReceiveHandle) { IsBackground = true, Name = "ReceiveHandle " + workerThreads }.Start();//创建处理线程
            }

            var scene = OnAddDefaultScene();
            DefaultScene = scene.Key;
            Scenes.TryAdd(scene.Key, scene.Value);
            OnStartupCompletedHandle();
            logList.Add("服务器启动成功!");
        }

        /// <summary>
        /// 流量统计线程
        /// </summary>
        protected void DebugThread()
        {
            while (IsRunServer)
            {
                Thread.Sleep(1000);
                try
                {
                    OnNetworkDataTraffic?.Invoke(sendAmount, sendCount, receiveAmount, receiveCount, resolveAmount);
                }
                finally
                {
                    sendCount = 0;
                    sendAmount = 0;
                    resolveAmount = 0;
                    receiveAmount = 0;
                    receiveCount = 0;
                }
            }
        }

        /// <summary>
        /// 调式输出信息线程
        /// </summary>
        protected void DebugLogThread()
        {
            while (IsRunServer)
            {
                Thread.Sleep(1);
                try
                {
                    while (logList.Count > 0)
                    {
                        Log?.Invoke(logList[0]);
                        logList.RemoveAt(0);
                    }
                } catch { }
            }
        }

        /// <summary>
        /// Udp每一帧
        /// </summary>
        private void UdpUpdate()
        {
            EndPoint remotePoint = Server.LocalEndPoint;
            byte[] buffer = new byte[65507];
            while (IsRunServer)
            {
                try
                {
                    int count = Server.ReceiveFrom(buffer, 0, buffer.Length, 0, ref remotePoint);
                    receiveAmount++;
                    receiveCount += count;
                    revdBufs.Enqueue(new ReceiveBuffer(buffer, count, remotePoint));
                }
                catch (Exception e)
                {
                    logList.Add($"主线程异常:{e.Message}");
                }
            }
        }

        /// <summary>
        /// 接收数据处理线程
        /// </summary>
        protected virtual void ReceiveHandle()
        {
            ReceiveBuffer receive = new ReceiveBuffer();
            while (IsRunServer)
            {
                Thread.Sleep(1);
                try
                {
                    if (revdBufs.TryDequeue(out receive))
                        UdpHandle(receive.buffer, receive.count, receive.remotePoint);
                }
                catch (Exception e)
                {
                    logList.Add($"处理异常:{e.Message}");
                }
            }
        }

        //udp数据处理线程
        private void UdpHandle(byte[] buffer, int count, EndPoint remotePoint)
        {
            if (Clients.ContainsKey(remotePoint))//在线客户端
            {
                ResolveBuffer(Clients[remotePoint], buffer, 0, count);//处理缓冲区数据
                return;
            }
            if (buffer[0] == 8 & count == 3)//退出程序指令
                return;
            if (buffer[0] == 5 & count == 1 | buffer[0] == 5 & count == 4)//buffer[0]=5:连接或心跳Ping指令 count=1:连接指令 count=4：心跳指令
            {
                Server.SendTo(new byte[] { 6, 0, 0 }, remotePoint);//心跳回应 或 连接回应
                return;
            }
            if (UnClients.ContainsKey(remotePoint))//未知客户端
            {
                int removeUnClient = ResolveUnBuffer(UnClients[remotePoint], remotePoint, buffer, count);
                if (removeUnClient == -1)//如果允许未知客户端进入服务器，则可以将此客户端从未知客户端字典种移除，并且添加此客户端到在线玩家集合中
                {
                    UnClients.TryRemove(remotePoint, out NetPlayer unClient1);
                    unClient1.Dispose();
                    unClient1 = null;
                }
                return;
            }
            if (UnClients.Count >= LineUp)//排队人数
            {
                exceededNumber++;
                logList.Add("未知客户端排队爆满,阻止连接人数: " + exceededNumber);
                return;
            }
            if (Clients.Count >= OnlineLimit)//服务器最大在线人数
            {
                blockConnection++;
                logList.Add("服务器爆满,阻止连接人数: " + blockConnection);
                return;
            }
            exceededNumber = 0;
            blockConnection = 0;
            NetPlayer unClient = new NetPlayer(GUID, remotePoint);
            OnHasConnectHandle?.Invoke(unClient);
            logList.Add("有客户端连接:" + remotePoint.ToString());
            int addUnClient = ResolveUnBuffer(unClient, remotePoint, buffer, count);//解析未知客户端数据
            if (addUnClient == 1)//如果需要添加未知客户端，则进行添加客户端， 为了下次访问时不需要重新创建对象， 性能优化
                UnClients.TryAdd(remotePoint, unClient);
        }

        //解析未知客户端数据缓冲区， 返回1：未知客户端表达没有通过， 返回-1：允许未知客户端进服务器，与其他客户端进行互动， 返回2：CC流量攻击行为
        private int ResolveUnBuffer(NetPlayer unPlayer, EndPoint remotePoint, byte[] buffer, int count)
        {
            byte cmd = buffer[0];//[0] = 网络命令整形数据
            int size = BitConverter.ToUInt16(buffer, 1);// {[1],[2]} 网络数据长度大小
            if (size + 3 != count)//如果数据协议不正确退出
                return 2;
            NetPlayer unClient = OnUnClientRequest(unPlayer, cmd, buffer, 3, count);
            if (unClient != null)//当有客户端连接时,如果允许用户添加此客户端
            {
                if (!unClient.RemotePoint.ContainsKey(GUID))
                    unClient.RemotePoint.Add(GUID, remotePoint);
                if (!Scenes.ContainsKey(unClient.sceneID))//如果非法场景ID则使用默认场景ID
                    unClient.sceneID = DefaultScene;
                unClient.RemotePoint[GUID] = remotePoint;//防止旧的端口号
                unClient.heart = 0;//心跳初始化
                Clients.TryAdd(remotePoint, unClient);//将网络玩家添加到集合中
                if (unClient.playerID == string.Empty)
                    unClient.playerID = Share.Random.Range(1000000,9999999).ToString();
                Players.TryAdd(unClient.playerID, unClient);//将网络玩家添加到集合中
                Scenes[DefaultScene].players.Add(unClient);//将网络玩家添加到主场景集合中
                unClient.Scene = Scenes[DefaultScene];//赋值玩家所在的场景实体
                unClient.Server = this;
                unClient.AddRpc(unClient);
                OnAddClientHandle?.Invoke(unClient);
                return -1;
            }
            return 1;
        }

        /// <summary>
        /// 解析网络数据包
        /// </summary>
        /// <param name="client">当前客户端</param>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="index">数据开始位置</param>
        /// <param name="count">数据大小</param>
        private void ResolveBuffer(NetPlayer client, byte[] buffer, int index, int count)
        {
            byte cmd = buffer[index];//0 = 网络命令整形数据
            int size = BitConverter.ToUInt16(buffer, index + 1);//1+2=3 帧头
            if (index + 3 + size == count)//如果数据完整 防止CC流量攻击
                OnRevdBufferHandle?.Invoke(client, cmd, buffer, index + 3, size);
        }

        /// <summary>
        /// 当处理缓冲区数据
        /// </summary>
        /// <param name="client">处理此客户端的数据请求</param>
        /// <param name="cmd">网络命令</param>
        /// <param name="buffer">缓冲区数据</param>
        /// <param name="index">数据开始索引</param>
        /// <param name="count">数据长度</param>
        protected void ReceiveBufferHandle(NetPlayer client, byte cmd, byte[] buffer, int index, int count)
        {
            resolveAmount++;
            switch (cmd)
            {
                case NetCmd.EntityRpc:
                    NetConvert.Deserialize(buffer, index, index + count, (func, pars) => {
                        NetDelegate rpc = client.Rpcs[func];
                        client.Server = this;
                        rpc.method.Invoke(rpc.target, pars);
                    });
                    break;
                case NetCmd.CallRpc:
                    NetConvert.Deserialize(buffer, index, index + count, (func, pars) => {
                        InvokeRpc(client, func, pars);
                    });
                    break;
                case NetCmd.LocalCmd:
                    Send(client, buffer, index - 3, count + 3);//发送数据到这个客户端
                    break;
                case NetCmd.SceneCmd:
                    Parallel.For(0, client.Scene.players.Count, i => //并行当前场景的客户端
                    {
                        if (client.Scene.players[i] == null)
                            return;
                        Send(client.Scene.players[i], buffer, index - 3, count + 3);//发送数据到这个客户端
                    });
                    break;
                case NetCmd.AllCmd:
                    Parallel.ForEach(Clients, player => //并行所有在线玩家
                    {
                        if (player.Value == null)
                            return;
                        Send(player.Value, buffer, index - 3, count + 3);//发送数据到这个客户端
                    });
                    break;
                case NetCmd.SendHeartbeat:
                    Send(client, NetCmd.RevdHeartbeat, new byte[1]);
                    break;
                case NetCmd.RevdHeartbeat:
                    client.heart = 0;
                    break;
                case NetCmd.QuitGame:
                    if (Clients.TryRemove(client.RemotePoint[GUID], out NetPlayer quitClient))
                        OnRemoveClientEvent?.Invoke(quitClient);
                    Players.TryRemove(client.playerID, out NetPlayer quitClient1);
                    if (Scenes.ContainsKey(client.sceneID))
                        Scenes[client.sceneID].players.Remove(client);
                    quitClient?.Dispose();
                    quitClient = null;
                    break;
                case NetCmd.SafeCall:
                    NetConvert.Deserialize(buffer, index, index + count, (func, pars) => {
                        InvokeRpc(client, func, pars);
                    });
                    break;
                default:
                    OnRevdCustomBufferHandle?.Invoke(client, cmd, buffer, index, count);
                    break;
            }
        }

        /// <summary>
        /// 调用Rpc函数
        /// </summary>
        /// <param name="client">客户端</param>
        /// <param name="func">函数名</param>
        /// <param name="pars">参数</param>
        private void InvokeRpc(NetPlayer client, string func, object[] pars)
        {
            Parallel.For(0, Rpcs.Count, (i) =>
            {
                if (Rpcs[i].method.Name != func)
                    return;
                try
                {
                    if (Rpcs[i].cmd == NetCmd.SafeCall)
                    {
                        var pars1 = new List<object>(pars.Length + 1) { client };
                        pars1.AddRange(pars);
                        Rpcs[i].method.Invoke(Rpcs[i].target, pars1.ToArray());
                    }
                    else
                    {
                        OnInvokeRpc(client);
                        Rpcs[i].method.Invoke(Rpcs[i].target, pars);
                    }
                }
                catch (Exception e)
                {
                    string str = "met:" + Rpcs[i].method + " pars:";
                    if (pars == null)
                        str += "null";
                    else
                        foreach (var p in pars)
                            str += p + " , ";
                    logList.Add(str + " -> " + e);
                }
            });
        }

        /// <summary>
        /// 未知客户端心跳包线程
        /// </summary>
        protected void UnHeartUpdate()
        {
            while (IsRunServer)
            {
                Thread.Sleep(5000);
                try
                {
                    UnHeartHandle();
                }
                catch { }
            }
        }

        /// <summary>
        /// 未知客户端心跳处理
        /// </summary>
        protected virtual void UnHeartHandle()
        {
            foreach (var client in UnClients.Values)
            {
                if (client.heart < 5)//有5次确认心跳包
                {
                    client.heart++;
                    Send(client, NetCmd.SendHeartbeat, new byte[1]);
                    continue;
                }
                UnClients.TryRemove(client.RemotePoint[GUID], out NetPlayer recClient);
                recClient.Dispose();
                logList.Add("移除未知客户端:" + client.RemotePoint[GUID].ToString());
                recClient = null;
            }
        }

        /// <summary>
        /// 心跳包线程
        /// </summary>
        protected void HeartUpdate()
        {
            while (IsRunServer)
            {
                Thread.Sleep(5000);
                try {
                    HeartHandle();
                } catch { }
            }
        }

        /// <summary>
        /// 心跳处理
        /// </summary>
        protected virtual void HeartHandle()
        {
            foreach (var client in Clients.Values)
            {
                if (client.heart < 5)//有5次确认心跳包
                {
                    client.heart++;
                    Send(client, NetCmd.SendHeartbeat, new byte[1]);
                    continue;
                }
                if (Clients.TryRemove(client.RemotePoint[GUID], out NetPlayer recClient))
                    OnRemoveClientEvent?.Invoke(recClient);
                Players.TryRemove(recClient.playerID, out NetPlayer recClient1);
                if (Scenes.ContainsKey(client.sceneID))
                    Scenes[client.sceneID].players.Remove(client);
                recClient?.Dispose();
                logList.Add("移除客户端: 玩家ID:" + client.playerID + " 玩家终端: " + client.RemotePoint[GUID].ToString());
                recClient = null;
                recClient1 = null;
            }
        }

        /// <summary>
        /// 创建网络场景 - 创建成功返回true， 创建失败返回false
        /// </summary>
        /// <param name="player">创建网络场景的玩家实体</param>
        /// <param name="sceneID">要创建的场景号或场景名称</param>
        /// <returns></returns>
        public bool CreateScene(NetPlayer player, string sceneID)
        {
            return CreateScene(player, sceneID, new NetScene());
        }

        /// <summary>
        /// 创建网络场景 - 创建成功返回true， 创建失败返回false
        /// </summary>
        /// <param name="player">创建网络场景的玩家实体</param>
        /// <param name="sceneID">要创建的场景号或场景名称</param>
        /// <param name="scene">创建场景的实体</param>
        /// <returns></returns>
        public bool CreateScene(NetPlayer player, string sceneID, NetScene scene)
        {
            if (!Scenes.ContainsKey(sceneID))
            {
                Scenes.TryAdd(sceneID, scene);
                if (!scene.players.Contains(player))
                    scene.players.Add(player);
                player.Scene = scene;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 加入场景 - 成功进入返回true，进入失败返回false
        /// </summary>
        /// <param name="player">要进入sceneID场景的玩家实体</param>
        /// <param name="sceneID">场景ID，要切换到的场景号或场景名称</param>
        /// <returns></returns>
        public bool JoinScene(NetPlayer player, string sceneID) => SwitchScene(player, sceneID);

        /// <summary>
        /// 进入场景 - 成功进入返回true，进入失败返回false
        /// </summary>
        /// <param name="player">要进入sceneID场景的玩家实体</param>
        /// <param name="sceneID">场景ID，要切换到的场景号或场景名称</param>
        /// <returns></returns>
        public bool EnterScene(NetPlayer player, string sceneID) => SwitchScene(player, sceneID);

        /// <summary>
        /// 切换场景 - 成功进入返回true，进入失败返回false
        /// </summary>
        /// <param name="player">要进入sceneID场景的玩家实体</param>
        /// <param name="sceneID">场景ID，要切换到的场景号或场景名称</param>
        /// <returns></returns>
        public bool SwitchScene(NetPlayer player, string sceneID)
        {
            if (Scenes.ContainsKey(sceneID))
            {
                var players = Scenes[player.sceneID].players;
                while (players.Contains(player))
                    players.Remove(player);
                Scenes[sceneID].players.Add(player);
                player.sceneID = sceneID;
                player.Scene = Scenes[sceneID];
                return true;
            }
            return false;
        }

        /// <summary>
        /// 移除场景
        /// </summary>
        /// <param name="sceneID">要移除的场景id</param>
        /// <returns></returns>
        public bool RemoveScene(string sceneID)
        {
            if (Scenes.ContainsKey(sceneID))
            {
                Scenes.TryRemove(sceneID, out NetScene scene);
                scene = null;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 将玩家从它当前所在的当前场景移除掉， 移除之后此客户端将会进入默认主场景
        /// </summary>
        /// <param name="player">要执行的玩家实体</param>
        /// <returns></returns>
        public bool RemoveScenePlayer(NetPlayer player)
        {
            if (Scenes.ContainsKey(player.sceneID))
            {
                var players = Scenes[player.sceneID].players;
                while (players.Contains(player))
                    players.Remove(player);
                if (!Scenes[DefaultScene].players.Contains(player))
                    Scenes[DefaultScene].players.Add(player);
                player.sceneID = DefaultScene;
                player.Scene = Scenes[DefaultScene];
                return true;
            }
            return false;
        }

        /// <summary>
        /// 从所有在线玩家字典中移除玩家实体
        /// </summary>
        /// <param name="player"></param>
        public void RemovePlayer(NetPlayer player) => RemoveClient(player);

        /// <summary>
        /// 从客户端字典中移除客户端
        /// </summary>
        /// <param name="client"></param>
        public void RemoveClient(NetPlayer client)
        {
            if (Clients.TryRemove(client.RemotePoint[GUID], out NetPlayer reClient))
                OnRemoveClientEvent?.Invoke(reClient);
            Players.TryRemove(client.playerID, out NetPlayer reClient1);
            if (Scenes.ContainsKey(client.sceneID))
            {
                var players = Scenes[client.sceneID].players;
                while (players.Contains(client))
                    players.Remove(client);
            }
            reClient?.Dispose();
            reClient = null;
        }

        /// <summary>
        /// 关闭服务器
        /// </summary>
        public virtual void Close()
        {
            IsRunServer = false;
            Server?.Dispose();
            Server?.Close();
            Instance?.Server?.Close();
            Instance = null;
        }

        /// <summary>
        /// 发送网络数据 UDP发送方式
        /// </summary>
        /// <param name="client">发送数据到的客户端</param>
        /// <param name="buffer">数据缓冲区</param>
        public void Send(NetPlayer client, byte[] buffer)
        {
            Send(client, NetCmd.CallRpc, buffer);
        }

        /// <summary>
        /// 发送网络数据 UDP发送方式
        /// </summary>
        /// <param name="client">发送数据到的客户端</param>
        /// <param name="fun">RPCFun函数</param>
        /// <param name="pars">RPCFun参数</param>
        public void Send(NetPlayer client, string fun, params object[] pars)
        {
            Send(client, NetCmd.CallRpc, fun, pars);
        }

        /// <summary>
        /// 网络多播, 发送数据到clients集合的客户端（并发, 有可能并行发送）
        /// </summary>
        /// <param name="clients">客户端集合</param>
        /// <param name="func">本地客户端rpc函数</param>
        /// <param name="pars">本地客户端rpc参数</param>
        public void Multicast(List<NetPlayer> clients, string func, params object[] pars)
        {
            Multicast(clients, NetCmd.CallRpc, func, pars);
        }

        /// <summary>
        /// 网络多播, 发送自定义数据到clients集合的客户端（并发, 有可能并行发送）
        /// </summary>
        /// <param name="clients">客户端集合</param>
        /// <param name="buffer">自定义字节数组</param>
        public void Multicast(List<NetPlayer> clients, byte[] buffer)
        {
            Multicast(clients, NetCmd.CallRpc, buffer);
        }

        /// <summary>
        /// 发送自定义网络数据
        /// </summary>
        /// <param name="client">发送到客户端</param>
        /// <param name="cmd">网络命令</param>
        /// <param name="buffer">数据缓冲区</param>
        public void Send(NetPlayer client, byte cmd, byte[] buffer)
        {
            buffer = Packing(cmd, buffer);
            Send(client, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 发送网络数据
        /// </summary>
        /// <param name="client">发送到的客户端</param>
        /// <param name="cmd">网络命令</param>
        /// <param name="func">RPCFun函数</param>
        /// <param name="pars">RPCFun参数</param>
        public void Send(NetPlayer client, byte cmd, string func, params object[] pars)
        {
            byte[] buffer = Packing(cmd, NetConvert.Serialize(func, pars));
            Send(client, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 网络多播, 发送数据到clients集合的客户端（并发, 有可能并行发送）
        /// </summary>
        /// <param name="clients">客户端集合</param>
        /// <param name="cmd">网络命令</param>
        /// <param name="func">本地客户端rpc函数</param>
        /// <param name="pars">本地客户端rpc参数</param>
        public void Multicast(List<NetPlayer> clients, byte cmd, string func, params object[] pars)
        {
            byte[] buffer = Packing(cmd, NetConvert.Serialize(func, pars));
            Parallel.ForEach(clients, client =>
            {
                if (client == null)
                    return;
                Send(client, buffer, 0, buffer.Length);
            });
        }

        /// <summary>
        /// 网络多播, 发送自定义数据到clients集合的客户端（并发, 有可能并行发送）
        /// </summary>
        /// <param name="clients">客户端集合</param>
        /// <param name="cmd">网络命令</param>
        /// <param name="buffer">自定义字节数组</param>
        public void Multicast(List<NetPlayer> clients, byte cmd, byte[] buffer)
        {
            buffer = Packing(cmd, buffer);
            Parallel.ForEach(clients, client =>
            {
                if (client == null)
                    return;
                Send(client, buffer, 0, buffer.Length);
            });
        }

        /// <summary>
        /// 网络统一包装
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private byte[] Packing(byte cmd, byte[] buffer)
        {
            using (MemoryStream stream = new MemoryStream(buffer.Length + 3))
            {
                stream.WriteByte(cmd);
                stream.Write(BitConverter.GetBytes((ushort)buffer.Length), 0, 2);
                stream.Write(buffer, 0, buffer.Length);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// 发送封装完成后的网络数据
        /// </summary>
        /// <param name="client">发送到的客户端</param>
        /// <param name="buffer">发送字节数组缓冲区</param>
        /// <param name="index">字节数组开始位置</param>
        /// <param name="count">字节数组长度</param>
        protected virtual void Send(NetPlayer client, byte[] buffer, int index, int count)
        {
            sendAmount++;
            sendCount += count;
            if (count < 65507 & index < count)
            {
                Server.SendTo(buffer, index, count, 0, client.RemotePoint[GUID]);
            }
        }
    }
}