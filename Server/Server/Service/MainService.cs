using System.Net.Sockets;
using System.Net;
using System;

namespace Server
{
    public delegate void OnStartHandler();
    public delegate void OnConnectHandler(Session c);
    public delegate void OnMessageHandler(Session c, MessageBuffer m);
    public delegate void OnDisconnectHandler(Session c);
    public delegate void OnExceptionHandler(Exception e);
    public delegate void OnPingHandler(Session c, int millis);
    public delegate void OnDebugHandler(string msg);
    public delegate void OnReceiveHandler(MessageInfo message);
    public delegate void OnAcceptHandler(Socket s);

    /// <summary>
    /// 主服务器 
    /// 主要负责---所有服务器的开关
    /// 主要负责---所有管理器的开关
    /// </summary>
    public class MainService
    {
        private TcpService mTcp;
        private UdpService mUdp;
        private KcpService mKcp;

        private UserManager mUserManager;
        private MatchManager mMatchManager;
        private BattleManager mBattleManager;

        public Mode mMode = Mode.LockStep;
        public Protocol mProtocol = Protocol.UDP;
        public TcpService Tcp { get { return mTcp; } }
        public UdpService Udp { get { return mUdp; } }
        public KcpService Kcp { get { return mKcp; } }

        private static MainService instance = null;
        public static MainService Instance { get { return instance; } }
        

        public MainService(Mode mode,Protocol protocol)
        {
            instance = this;
            mTcp = new TcpService(ServerConfig.TCP_PORT);
            mMode = mode;
            mProtocol = protocol;

            if (mProtocol==Protocol.KCP) mKcp = new KcpService(ServerConfig.UDP_PORT);
            else  mUdp = new UdpService(ServerConfig.UDP_PORT);

            mUserManager = new UserManager();
            mMatchManager = new MatchManager();
            mBattleManager = new BattleManager();

            Debug.Log(string.Format("Server start success,mode={0} ip={1} tcp port={2} udp port={3}",
                mMode.ToString(), GetLocalIP(), ServerConfig.TCP_PORT, ServerConfig.UDP_PORT), ConsoleColor.Green);
        }

        public bool IsActive
        {
            get
            {
                return mTcp.IsActive && ((mUdp != null && mUdp.IsActive) || (mKcp != null && mKcp.IsActive));
            }
        }

        public void Start()
        {
            mTcp.Listen();
            if (mUdp != null)
            {
                mUdp.Listen();
            }
            else if (mKcp != null)
            {
                mKcp.Listen();
            }
        }

        public void Close()
        {
            mTcp.Close();
            mUdp.Close();
            mKcp.Close();
        }

        /// <summary>
        /// 获取本机IP地址
        /// </summary>
        /// <returns>本机IP地址</returns>
        public static string GetLocalIP()
        {
            try
            {
                string HostName = Dns.GetHostName(); //得到主机名
                IPHostEntry IpEntry = Dns.GetHostEntry(HostName);
                for (int i = 0; i < IpEntry.AddressList.Length; i++)
                {
                    //从IP地址列表中筛选出IPv4类型的IP地址
                    //AddressFamily.InterNetwork表示此IP为IPv4,
                    //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                    if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        return IpEntry.AddressList[i].ToString();
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
