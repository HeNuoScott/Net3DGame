
namespace Server
{
    public class ServerManager : QF.MonoSingleton<ServerManager>
    {
        public NetGameServer server = new NetGameServer();//������ʵ��
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