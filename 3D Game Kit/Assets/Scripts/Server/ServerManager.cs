
namespace Server
{
    using Net.Server;
    using QF;

    [MonoSingletonPath("[GameDesigner]/ServerManager")]
    public class ServerManager : MonoSingleton<ServerManager>
    {
        public NetGameServer server = new NetGameServer();//������ʵ��
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

        //���з�����
        public void StartServer()
        {
            server.Start(port, workerThreads);
        }

        //�رշ�����
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