namespace Net.Server
{
    using Net.Share;
    using System;

    /// <summary>
    /// 网络服务器核心类 2019.6.26
    /// </summary>
    [Serializable]
    public class NetServer : NetServerBase
    {
        /// <summary>
        /// 添加网络Rpc
        /// </summary>
        /// <param name="target">注册的对象实例</param>
        public void AddRpcHandle(object target)
        {
            NetBehaviour.AddRpcs(this, target);
        }

        /// <summary>
        /// 添加网络Rpc参数类型， 添加后即可使用Send发送参数为T的对象实例
        /// </summary>
        /// <typeparam name="T">rpc参数类型</typeparam>
        public void AddNetType<T>()
        {
            NetConvert.AddNetworkType(typeof(T));
        }
    }

    /// <summary>
    /// 网络服务器核心类 2019.6.26
    /// </summary>
    [Serializable]
    public class UdpServer : NetServerBase
    {
        /// <summary>
        /// 添加网络Rpc
        /// </summary>
        /// <param name="target">注册的对象实例</param>
        public void AddRpcHandle(object target)
        {
            NetBehaviour.AddRpcs(this, target);
        }

        /// <summary>
        /// 添加网络Rpc参数类型， 添加后即可使用Send发送参数为T的对象实例
        /// </summary>
        /// <typeparam name="T">rpc参数类型</typeparam>
        public void AddNetType<T>()
        {
            NetConvert.AddNetworkType(typeof(T));
        }
    }
}
