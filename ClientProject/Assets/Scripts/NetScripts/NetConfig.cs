
namespace Network
{
    public delegate void OnConnectHandler();
    public delegate void OnMessageHandler(MessageBuffer msg);
    public delegate void OnDisconnectHandler();
    public delegate void OnPingHandler(int m);
    public delegate void OnDebugHandler(string msg);
    public delegate void OnAcceptPollHandler(int sock);

    public static class NetConfig
    {
        public static string serverIP;
        public static readonly int TCP_PORT = 1255;
        public static readonly int UDP_PORT = 1337;
        public static readonly int frameTime = 66;
    }
    /// <summary>
    /// 模式
    /// </summary>
    public enum Mode
    {
        LockStep,
        Optimistic,
    }

    /// <summary>
    /// 协议
    /// </summary>
    public enum Protocol
    {
        UDP,
        KCP,
    }
}