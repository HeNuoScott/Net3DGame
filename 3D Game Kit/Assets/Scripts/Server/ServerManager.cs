
namespace Server
{
    using Net.Server;
    using QF;

    [MonoSingletonPath("[GameDesigner]/ServerManager")]
    public class ServerManager : MonoSingleton<ServerManager>
    {
        public NetGameServer server = new NetGameServer();//服务器实体
        public int port = 666;
        public int workerThreads = 0;
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