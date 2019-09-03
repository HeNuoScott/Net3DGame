
namespace Server
{
    public class ServerManager : QF.MonoSingleton<ServerManager>
    {
        public NetGameServer server = new NetGameServer();//服务器实体
        public int port = 666;
        public int workerThreads = 5;
        public bool runServer;

        public void OnClick()
        {
            runServer = !runServer;
            if (runServer)
            {
                StartServer();
            }
            else
            {
                StopServer();
            }
        }

        //运行服务器
        public void StartServer()
        {
            server.Start(port, workerThreads);
        }

        //关闭服务器
        public void StopServer()
        {
            server.Close();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            StopServer();
        }
    }
}