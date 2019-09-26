namespace Net.Server
{
    using Net.Share;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection;

    /// <summary>
    /// 网络玩家 - 当客户端连接服务器后都会为每个客户端生成一个网络玩家对象，(玩家对象由服务器管理) 2019.9.9
    /// </summary>
    public class NetPlayer : IDisposable
    {
        /// <summary>
        /// Tcp套接字
        /// </summary>
        public Socket Client { get; private set; }
        /// <summary>
        /// Tcp网络流
        /// </summary>
        public NetworkStream Stream { get; private set; }
        /// <summary>
        /// 存储UDP客户端终端
        /// </summary>
        public Dictionary<Guid, EndPoint> RemotePoint { get; set; } = new Dictionary<Guid, EndPoint>();
        /// <summary>
        /// 此玩家所在的场景ID
        /// </summary>
        public string sceneID = "MainScene";
        /// <summary>
        /// 客户端玩家的标识
        /// </summary>
        public string playerID = string.Empty;
        /// <summary>
        /// 玩家所在的场景实体
        /// </summary>
        public NetScene Scene { get; set; }
        /// <summary>
        /// 玩家rpc
        /// </summary>
        public Dictionary<string, NetDelegate> Rpcs { get; set; } = new Dictionary<string, NetDelegate>();
        /// <summary>
        /// 网络服务器rpc临时对象
        /// </summary>
        public NetServerBase Server { get; set; }
        /// <summary>
        /// 临时客户端持续时间: (内核使用):
        /// 未知客户端连接服务器, 长时间未登录账号, 未知客户端临时内存对此客户端回收, 并强行断开此客户端连接
        /// </summary>
        public DateTime LastTime { get; set; }

        /// <summary>
        /// 跳动的心
        /// </summary>
        internal byte heart = 0;

        #region 创建网络客户端(玩家)
        /// <summary>
        /// 构造网络客户端
        /// </summary>
        public NetPlayer(){ }

        /// <summary>
        /// 构造网络客户端，Tcp
        /// </summary>
        /// <param name="serverID">服务器的实例ID， 如果你的服务器有场景服务器，大厅服务器，公告服务器，数据库服务器的时候，就会用到guid来识别要使用哪个服务器实例来发送的数据</param>
        /// <param name="client">客户端套接字</param>
        public NetPlayer(Guid serverID, Socket client)
        {
            Client = client;
            Stream = new NetworkStream(client);
            RemotePoint.Add(serverID, client.RemoteEndPoint);
        }

        /// <summary>
        /// 构造网络客户端
        /// </summary>
        /// <param name="serverID">服务器的实例ID， 如果你的服务器有场景服务器，大厅服务器，公告服务器，数据库服务器的时候，就会用到guid来识别要使用哪个服务器实例来发送的数据</param>
        /// <param name="remotePoint"></param>
        public NetPlayer(Guid serverID, EndPoint remotePoint)
        {
            RemotePoint.Add(serverID, remotePoint);
        }
        #endregion

        #region 客户端释放内存
        /// <summary>
        /// 析构网络客户端
        /// </summary>
        ~NetPlayer()
        {
            Client?.Close();
            Stream?.Close();
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            Client?.Close();
            Stream?.Close();
        }
        #endregion

        #region 客户端(玩家)Rpc(远程过程调用)处理
        /// <summary>
        /// 添加远程过程调用函数,从对象进行收集
        /// </summary>
        /// <param name="target"></param>
        /// <param name="append">可以重复添加rpc?</param>
        public void AddRpc(object target, bool append = false)
        {
            if (!append)
                foreach (var o in Rpcs.Values)
                    if (o.target == target)
                        return;
            
            foreach (MethodInfo info in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var rpc = info.GetCustomAttribute<RPCFun>();
                if (rpc == null)
                    continue;
                if (Rpcs.ContainsKey(info.Name))
                {
                    Server.logList.Add($"添加客户端私有Rpc错误！Rpc方法{info.Name}使用同一函数名，这是不允许的，字典键值无法添加相同的函数名！");
                    continue;
                }
                Rpcs.Add(info.Name, new NetDelegate(target, info, rpc.cmd));
            }
        }

        /// <summary>
        /// 移除网络远程过程调用函数
        /// </summary>
        /// <param name="target">移除的rpc对象</param>
        public void RemoveRpc(object target)
        {
            foreach (var rpc in Rpcs)
            {
                if (rpc.Value.target == target | rpc.Value.target.Equals(target) | rpc.Value.target.Equals(null) | rpc.Value.method.Equals(null))
                {
                    Rpcs.Remove(rpc.Key);
                }
            }
        }
        #endregion
        
        #region 客户端(玩家)回调给自己
        /// <summary>
        /// 发送网络数据 UDP发送方式
        /// </summary>
        /// <param name="buffer">数据缓冲区</param>
        public void Send(byte[] buffer)
        {
            Server.Send(this, buffer);
        }

        /// <summary>
        /// 发送网络数据 UDP发送方式
        /// </summary>
        /// <param name="fun">RPCFun函数</param>
        /// <param name="pars">RPCFun参数</param>
        public void Send(string fun, params object[] pars)
        {
            Server.Send(this, fun, pars);
        }

        /// <summary>
        /// 网络多播, 发送数据到clients集合的客户端（并发, 有可能并行发送）
        /// </summary>
        /// <param name="clients">客户端集合</param>
        /// <param name="func">本地客户端rpc函数</param>
        /// <param name="pars">本地客户端rpc参数</param>
        public void Multicast(List<NetPlayer> clients, string func, params object[] pars)
        {
            Server.Multicast(clients, func, pars);
        }

        /// <summary>
        /// 网络多播, 发送自定义数据到clients集合的客户端（并发, 有可能并行发送）
        /// </summary>
        /// <param name="clients">客户端集合</param>
        /// <param name="buffer">自定义字节数组</param>
        public void Multicast(List<NetPlayer> clients, byte[] buffer)
        {
            Server.Multicast(clients, buffer);
        }

        /// <summary>
        /// 发送自定义网络数据
        /// </summary>
        /// <param name="cmd">网络命令</param>
        /// <param name="buffer">数据缓冲区</param>
        public void Send(byte cmd, byte[] buffer)
        {
            Server.Send(this, cmd, buffer);
        }

        /// <summary>
        /// 发送网络数据
        /// </summary>
        /// <param name="cmd">网络命令</param>
        /// <param name="func">RPCFun函数</param>
        /// <param name="pars">RPCFun参数</param>
        public void Send(byte cmd, string func, params object[] pars)
        {
            Server.Send(this, cmd, func, pars);
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
            Server.Multicast(clients, cmd, func, pars);
        }

        /// <summary>
        /// 网络多播, 发送自定义数据到clients集合的客户端（并发, 有可能并行发送）
        /// </summary>
        /// <param name="clients">客户端集合</param>
        /// <param name="cmd">网络命令</param>
        /// <param name="buffer">自定义字节数组</param>
        public void Multicast(List<NetPlayer> clients, byte cmd, byte[] buffer)
        {
            Server.Multicast(clients, cmd, buffer);
        }
        #endregion

        #region 服务器发送网络数据请求
        /// <summary>
        /// 发送网络数据 UDP发送方式
        /// </summary>
        /// <param name="client">发送数据到的客户端</param>
        /// <param name="buffer">数据缓冲区</param>
        public void Send(NetPlayer client, byte[] buffer)
        {
            Server.Send(client, buffer);
        }

        /// <summary>
        /// 发送网络数据 UDP发送方式
        /// </summary>
        /// <param name="client">发送数据到的客户端</param>
        /// <param name="fun">RPCFun函数</param>
        /// <param name="pars">RPCFun参数</param>
        public void Send(NetPlayer client, string fun, params object[] pars)
        {
            Server.Send(client, fun, pars);
        }

        /// <summary>
        /// 发送自定义网络数据
        /// </summary>
        /// <param name="client">发送到客户端</param>
        /// <param name="cmd">网络命令</param>
        /// <param name="buffer">数据缓冲区</param>
        public void Send(NetPlayer client, byte cmd, byte[] buffer)
        {
            Server.Send(client, cmd, buffer);
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
            Server.Send(client, cmd, func, pars);
        }
        #endregion

        #region 客户端数据处理函数
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
        public virtual NetPlayer OnUnClientRequest(byte cmd, byte[] buffer, int index, int count)
        {
            return this;
        }

        /// <summary>
        /// 当接收到客户端自定义数据请求,在这里可以使用你自己的网络命令，系列化方式等进行解析网络数据。（你可以在这里使用ProtoBuf或Json来解析网络数据）
        /// </summary>
        /// <param name="cmd">网络命令</param>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="index">数据的开始索引</param>
        /// <param name="count">数据长度</param>
        public virtual void OnReceiveBuffer(byte cmd, byte[] buffer, int index, int count) { }

        /// <summary>
        /// 当服务器判定客户端为断线或连接异常时，移除客户端时调用
        /// </summary>
        public virtual void OnRemoveClient() { }
        #endregion

        #region 网络场景处理
        /// <summary>
        /// 创建网络场景 - 创建成功返回true， 创建失败返回false
        /// </summary>
        /// <param name="sceneID">要创建的场景号或场景名称</param>
        /// <returns></returns>
        public bool CreateScene(string sceneID)
        {
            return Server.CreateScene(this, sceneID, new NetScene());
        }

        /// <summary>
        /// 创建网络场景 - 创建成功返回true， 创建失败返回false
        /// </summary>
        /// <param name="sceneID">要创建的场景号或场景名称</param>
        /// <param name="scene">创建场景的实体</param>
        /// <returns></returns>
        public bool CreateScene(string sceneID, NetScene scene)
        {
            return Server.CreateScene(this, sceneID, scene);
        }

        /// <summary>
        /// 加入场景 - 成功进入返回true，进入失败返回false
        /// </summary>
        /// <param name="sceneID">场景ID，要切换到的场景号或场景名称</param>
        /// <returns></returns>
        public bool JoinScene(string sceneID) => SwitchScene(sceneID);

        /// <summary>
        /// 进入场景 - 成功进入返回true，进入失败返回false
        /// </summary>
        /// <param name="sceneID">场景ID，要切换到的场景号或场景名称</param>
        /// <returns></returns>
        public bool EnterScene(string sceneID) => SwitchScene(sceneID);

        /// <summary>
        /// 切换场景 - 成功进入返回true，进入失败返回false
        /// </summary>
        /// <param name="sceneID">场景ID，要切换到的场景号或场景名称</param>
        /// <returns></returns>
        public bool SwitchScene(string sceneID)
        {
            return Server.SwitchScene(this, sceneID);
        }

        /// <summary>
        /// 移除场景
        /// </summary>
        /// <param name="sceneID">要移除的场景id</param>
        /// <returns></returns>
        public bool RemoveScene(string sceneID)
        {
            return Server.RemoveScene(sceneID);
        }

        /// <summary>
        /// 将玩家从它当前所在的当前场景移除掉， 移除之后此客户端将会进入默认主场景
        /// </summary>
        /// <returns></returns>
        public bool RemoveScenePlayer()
        {
            return Server.RemoveScenePlayer(this);
        }

        /// <summary>
        /// 从所有在线玩家字典中移除玩家实体
        /// </summary>
        public void RemovePlayer() => RemoveClient();

        /// <summary>
        /// 从客户端字典中移除客户端
        /// </summary>
        public void RemoveClient()
        {
            Server.RemoveClient(this);
        }
        #endregion
    }
}