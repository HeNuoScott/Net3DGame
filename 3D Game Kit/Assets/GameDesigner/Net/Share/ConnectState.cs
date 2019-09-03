﻿namespace Net.Share
{
    /// <summary>
    /// 网络连接状态
    /// </summary>
	public enum ConnectState : byte
	{
        /// <summary>
        /// 无状态
        /// </summary>
        None,
        /// <summary>
        /// 连接成功
        /// </summary>
        Connected,
        /// <summary>
        /// 连接失败
        /// </summary>
        ConnectFailed,
        /// <summary>
        /// 尝试连接
        /// </summary>
        TryToConnect,
        /// <summary>
        /// 断开连接
        /// </summary>
        Disconnect,
        /// <summary>
        /// 连接中断 (连接异常)
        /// </summary>
        ConnectLost,
        /// <summary>
        /// 连接已被关闭
        /// </summary>
        ConnectClosed,
        /// <summary>
        /// 连接正常
        /// </summary>
        Connection,
        /// <summary>
        /// 断线重连成功
        /// </summary>
        Reconnect,
    }
}