namespace Server
{
    using Net.Server;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Net;

    public class NetGameServer : UdpServer
    {
        /// <summary>
        /// ������������
        /// ���������з��������ڶ��߳������   ���̲߳���ֱ�ӷ���unity��������Ժͷ���
        /// </summary>
        private Player RequestPlayer;
        public int OnLineCount{get { return Players.Count; }}
        public static event Action<string> ServerLog;
        public static void DebugLog(string str) => ServerLog?.Invoke(str);

        protected override void OnStarting()
        {
            DebugLog("��ʼ����������");
            this.Log += (msg) => { DebugLog(msg); };
            //�������ת������
            Net.Share.NetConvert.AddNetworkType<NetScene>();
            Net.Share.NetConvert.AddNetworkType<List<NetScene>>();
        }
        protected override void OnStartupCompleted()
        {
            DebugLog("�������������");      
        }
        protected override void OnHasConnect(NetPlayer client)
        {
            DebugLog($"�ͻ��ˣ�{client.RemotePoint[GUID].ToString()}����......");
        }
        protected override void OnRemoveClient(NetPlayer client)
        {
            DebugLog($"�ͻ��ˣ�{client.playerID.ToString()}�Ͽ�����......");
        }
        protected override void OnReceiveBuffer(NetPlayer client, byte cmd, byte[] buffer, int index, int count)
        {
            //DebugLog("����ԭ��byte[]����:");
            base.OnReceiveBuffer(client, cmd, buffer, index, count);
        }
        //�û����ÿͻ��˵�Rpc����ʱ����
        protected override void OnInvokeRpc(NetPlayer client)
        {
            RequestPlayer = client as Player;
        }
        //���û���������ʱ����
        protected override NetPlayer OnUnClientRequest(NetPlayer unClient, byte cmd, byte[] buffer, int index, int count)
        {
            Net.Share.NetConvert.FuncData func = Net.Share.NetConvert.DeserializeFunc(buffer, index, count);
            switch (func.func)
            {
                case "Register":
                    Register(unClient, func.pars[0].ToString(), func.pars[1].ToString());
                    break;
                case "Login":
                    return Login(unClient, func.pars[0].ToString(), func.pars[1].ToString());
            }
            //�����¼ʧ�ܷ���null���ͻ��˲���ӵ��ֵ䵱�У���Ϣ���Ͳ����룩
            return null;
        }
        //�������Ĭ�ϳ���
        protected override KeyValuePair<string, NetScene> OnAddDefaultScene()
        {
            DebugLog("�������Ĭ�ϳ���");
            //new Thread(SendSceneInfoToLobbay) { IsBackground = true, Name = "SendSceneInfoToLobbay" }.Start();
            return base.OnAddDefaultScene();
        }

        public void SendSceneInfoToLobbay()
        {
            while (IsRunServer)
            {
                Thread.Sleep(1000);
                try
                {
                    List<NetPlayer> players = Scenes[DefaultScene].players;
                    List<NetScene> netScenes = GetScenes<NetScene>();
                    if (players.Count > 0) Multicast(players, "OnLobbayReceiveScenesInfo", netScenes);
                }
                finally
                {

                }
            }
        }

        //------------------------------------------------------RPC---------------------------------------------------------------------

        //ע��
        private void Register(NetPlayer unClient, string acc, string pass)
        {
            //����˺Ŵ��ڣ�����ע��
            if (DataBase.Users.ContainsKey(acc))
            {
                Send(unClient, "RegisterResult", "�˺��Ѵ��ڣ�ע��ʧ�ܣ�");
                return;
            }
            Player player = new Player();
            player.acc = acc;
            player.pass = pass;
            player.playerID = acc;//���˺ű�ʶ��ҵ�Ψһ��ʶID

            DataBase.Add(player);

            DebugLog($"�ͻ��ˣ�{player.acc}�˺�ע��ɹ���......");
            Send(unClient, "RegisterResult", "�˺�ע��ɹ���");

        }
        //��¼
        private Player Login(NetPlayer unClient, string acc, string pass)
        {
            //����˺Ŵ��ڲ��ܵ�½
            if (DataBase.Users.ContainsKey(acc))
            {
                Player player = DataBase.Users[acc];
                if (player.pass == pass)
                {
                    //�������ߵĿͻ���  ����Ѿ���¼��״̬
                    if (Players.ContainsKey(acc))
                    {
                        NetPlayer OnLinePlayer = Players[acc];
                        Send(OnLinePlayer, "OtherLogin");//�����������
                        Send(unClient, "LoginResult", false, "�˺��Ѿ���¼");
                        return null;
                    }
                    DebugLog($"�ͻ��ˣ�{player.acc}�˺ŵ�½�ɹ���......");
                    //���δ��¼��״̬
                    Send(unClient, "LoginResult", true, "��½�ɹ�");
                    return player;
                }
            }
            Send(unClient, "LoginResult", false, "�˺Ż��������󣬵�¼ʧ�ܣ�");
            return null;
        }

        [Net.Share.Rpc]
        public void CreatePlayer()
        {
            //�����ͻ����������
            Send(RequestPlayer, "CreateLocalPlayer", RequestPlayer.acc);
            //ͨ����ҵ�ID�ҵ���������,������������
            List<NetPlayer> players = Scenes[RequestPlayer.sceneID].players;
            //(�㲥)���͸������ͻ������˽����� ͬһ������ ,�ÿͻ��˴�����Ӧ�� ��Ҷ���
            Multicast(players, "CreateOtherPlayer", RequestPlayer.acc);
            //��ȡ���������е����,���Լ��Ŀͻ���Ҳ�������ǵ���Ҷ���
            foreach (var player in Scenes[RequestPlayer.sceneID].players)
            {
                Send(RequestPlayer, "CreateOtherPlayer", player.playerID);
            }
        }

        [Net.Share.Rpc]
        public void CetUserSelfInfo()
        {
            Send(RequestPlayer, "CetUserSelfInfoResult", RequestPlayer.acc, RequestPlayer.acc);
        }

        [Net.Share.Rpc]
        public void CreateRoom(string roomName,int sceneCapacity)
        {
            if (roomName==string.Empty)
            {
                Send(RequestPlayer, "CreateRoomResult", false, "����������Ϊ��");
            }
            else if (Scenes.ContainsKey(roomName))
            {
                Send(RequestPlayer, "CreateRoomResult", false, "�����ķ����Ѿ�����");
            }
            else
            {
                Scenes.TryAdd(roomName, new NetScene(sceneCapacity) { players = new List<NetPlayer>() { RequestPlayer } });
                RequestPlayer.sceneID = roomName;
                DebugLog($"�ͻ��ˣ�{RequestPlayer.acc}������:{roomName}:����");
                Send(RequestPlayer, "CreateRoomResult", true, "��������ɹ�");
            }

        }

        [Net.Share.Rpc]
        public void ExitRoom()
        {
            var scene = Scenes[RequestPlayer.sceneID];
            scene.players.Remove(RequestPlayer);
            Multicast(scene.players, "RemovePlayer", RequestPlayer.acc);
            if (scene.players.Count<=0)
            {
                Scenes.TryRemove(RequestPlayer.sceneID, out NetScene netScene);
                DebugLog(RequestPlayer.sceneID+":�����ɢ");
            }
            RequestPlayer.sceneID = DefaultScene;
        }

        [Net.Share.Rpc]
        public void JoinRoom(string roomName)
        {
            if (!Scenes.ContainsKey(roomName))
            {
                Send(RequestPlayer, "JoinRoomResult", false, "����ķżٲ����ڻ��Ѿ���ɢ!");
            }
            else if (Scenes[roomName].sceneCapacity == Scenes[roomName].SceneNumber)
            {
                Send(RequestPlayer, "JoinRoomResult", false, "��������!");
            }
            else
            {
                Scenes[roomName].players.Add(RequestPlayer);
                RequestPlayer.sceneID = roomName;
                Send(RequestPlayer, "JoinRoomResult", true, "���뷿��ɹ�");
            }
        }



    }
}