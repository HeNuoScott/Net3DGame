namespace Net.Share
{
    using Net.Server;
    using System;
    using System.Net;

    /// <summary>
    /// 网络服务器事件处理
    /// </summary>
    public interface IServerEventHandle
    {
        /// <summary>
        /// 开始运行服务器事件
        /// </summary>
        event Action OnStartingHandle;
        /// <summary>
        /// 服务器启动成功事件
        /// </summary>
        event Action OnStartupCompletedHandle;
        /// <summary>
        /// 当前有客户端连接触发事件
        /// </summary>
        event Action<NetPlayer> OnHasConnectHandle;
        /// <summary>
        /// 当添加客户端到所有在线的玩家集合中触发的事件
        /// </summary>
        event Action<NetPlayer> OnAddClientHandle;
        /// <summary>
        /// 当开始调用rpc函数事件, 多线程时5%不安全
        /// </summary>
        event Action<NetPlayer> OnInvokeRpcHandle;
        /// <summary>
        /// 当接收到网络数据处理事件
        /// </summary>
        event RevdBufferHandle OnRevdBufferHandle;
        /// <summary>
        /// 当接收自定义网络数据事件
        /// </summary>
        event RevdBufferHandle OnRevdCustomBufferHandle;
        /// <summary>
        /// 当移除客户端时触发事件
        /// </summary>
        event Action<NetPlayer> OnRemoveClientEvent;
        /// <summary>
        /// 当统计网络流量时触发
        /// </summary>
        event NetworkDataTraffic OnNetworkDataTraffic;
        /// <summary>
        /// 输出日志
        /// </summary>
        event Action<string> Log;
    }
}