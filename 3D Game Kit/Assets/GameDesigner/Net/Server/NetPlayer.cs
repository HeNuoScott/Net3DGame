namespace Net.Server
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    /// 网络玩家
    /// </summary>
    public class NetPlayer
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
        public string playerID = "";
        /// <summary>
        /// 玩家所在的场景实体
        /// </summary>
        public NetScene Scene { get; set; }
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
    }
}