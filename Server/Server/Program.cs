using System;


namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Program p = new Program();
        }

        public Program()
        {
            Debug.Log("服务器启动，请选择服务器运行模式，按Enter确定");
            Debug.Log("1. lockstep mode.");
            Debug.Log("2. optimistic mode.");
            Mode mMode = Console.ReadLine() == "1" ? Mode.LockStep : Mode.Optimistic;

            Debug.Log("服务器启动，请选战斗数据传输协议，按Enter确定");
            Debug.Log("1. use udp.");
            Debug.Log("2. use kcp.");
            Protocol mProtocol = Console.ReadLine() == "1" ? Protocol.UDP : Protocol.KCP;

            MainService mService = new MainService(ServerConfig.TCP_PORT, ServerConfig.UDP_PORT, mMode, mProtocol);

            mService.Start();
        }
    }
}
