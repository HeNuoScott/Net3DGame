using System.Net.Sockets;
using Server;
using System;

public static class ServerConfig
{
    public static int PVP_Number = 2;
    public static Mode MODE = Mode.LockStep;
    public static Protocol PROTO = Protocol.UDP;
    public const int TCP_PORT = 1255;
    public const int UDP_PORT = 1337;

    public const int FRAME_INTERVAL = 66; //帧时间 毫秒
}

public enum Mode
{
    /// <summary>
    /// 帧同步
    /// </summary>
    LockStep,
    /// <summary>
    /// 乐观帧同步
    /// </summary>
    Optimistic,
}

public enum Protocol
{
    /// <summary>
    /// UDP协议
    /// </summary>
    UDP,
    /// <summary>
    /// 可靠UDP协议
    /// </summary>
    KCP,
}

public enum UserState
{
    OffLine,
    OnLine,
    Lobbying,
    Matching,
    Battleing,
}