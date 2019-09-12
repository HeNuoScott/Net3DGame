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
        /// 跳动的心
        /// </summary>
        internal byte heart = 0;

        /// <summary>
        /// 构造网络客户端
        /// </summary>
        public NetPlayer(){ }

        /// <summary>
        /// 构造网络客户端，Tcp
        /// </summary>
        /// <param name="serverID">服务器的实例ID， 如果你的服务器有场景服务器，大厅服务器，公告服务器，数据库服务器的时候，就会用到guid来识别服务器实例所发送的终端点</param>
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
        /// <param name="serverID">服务器的实例ID， 如果你的服务器有场景服务器，大厅服务器，公告服务器，数据库服务器的时候，就会用到guid来识别服务器实例所发送的终端点</param>
        /// <param name="remotePoint"></param>
        public NetPlayer(Guid serverID, EndPoint remotePoint)
        {
            RemotePoint.Add(serverID, remotePoint);
        }

        /// <summary>
        /// 析构网络客户端
        /// </summary>
        ~NetPlayer()
        {
            Client?.Close();
            Stream?.Close();
        }

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

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            Client?.Close();
            Stream?.Close();
        }

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
    }
}