using System.Net.Sockets;
using Server;
using System;

public static class ServerConfig
{
    public static int PVP_Number = 2;
    public const int TCP_PORT = 1255;
    public const int UDP_PORT = 1337;

    public const int FRAME_INTERVAL = 66; //帧时间 毫秒
}

public enum Mode
{
    LockStep,
    Optimistic,
}

public enum Protocol
{
    UDP,
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