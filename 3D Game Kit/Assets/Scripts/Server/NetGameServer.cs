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
    using Net.Share;

    public class NetGameServer : UdpServer
    {
        /// <summary>
        /// ������������
        /// ���������з��������ڶ��߳������   ���̲߳���ֱ�ӷ���unity��������Ժͷ���
        /// </summary>
        private ServerPlayer RequestPlayer;
        public int OnLineCount{get { return Players.Count; }}
        public static event Action<string> ServerLog;
        public static void DebugLog(string str) => ServerLog?.Invoke(str);

        protected override void OnStarting()
        {
            DebugLog("��ʼ����������");
            this.Log += (msg) => { DebugLog(msg); };
            //�������ת������
            Net.Share.NetConvert.AddNetworkType<ChatMsg>();
            Net.Share.NetConvert.AddNetworkType<RoomOperationCode>();
            Net.Share.NetConvert.AddNetworkType<RoomInfo>();
            Net.Share.NetConvert.AddNetworkType<ServerPlayer>();
            Net.Share.NetConvert.AddNetworkType<Dictionary<string, RoomInfo>>();
            
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

            var scene = Scenes[client.sceneID];
            scene.players.Remove(client);
            Multicast(scene.players, "RemovePlayer", RequestPlayer.acc);
            if (scene.players.Count <= 0 && client.sceneID != DefaultScene)
            {
                Scenes.TryRemove(client.sceneID, out NetScene netScene);
                DebugLog(client.sceneID + ":�����ɢ");
            }
        }
        protected override void OnReceiveBuffer(NetPlayer client, byte cmd, byte[] buffer, int index, int count)
        {
            //DebugLog("����ԭ��byte[]����:");
            base.OnReceiveBuffer(client, cmd, buffer, index, count);
        }
        //�û����ÿͻ��˵�Rpc����ʱ����
        protected override void OnInvokeRpc(NetPlayer client)
        {
            RequestPlayer = client as ServerPlayer;
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
            new Thread(SendSceneInfoToLobbay) { IsBackground = true, Name = "SendSceneInfoToLobbay" }.Start();
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

                    Dictionary<string, RoomInfo> netSceneInfo = new Dictionary<string, RoomInfo>();
                    foreach (var item in Scenes)
                    {
                        netSceneInfo.Add(item.Key, new RoomInfo()
                        {
                            roomName = item.Key,
                            roomCapacity = item.Value.sceneCapacity,
                            roomNumber = item.Value.SceneNumber
                        });
                    }

                    if (players.Count > 0) Multicast(players, "OnLobbayReceiveScenesInfo", netSceneInfo);
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
            ServerPlayer player = new ServerPlayer();
            player.acc = acc;
            player.pass = pass;
            player.playerID = acc;//���˺ű�ʶ��ҵ�Ψһ��ʶID

            DataBase.Add(player);

            DebugLog($"�ͻ��ˣ�{player.acc}�˺�ע��ɹ���......");
            Send(unClient, "RegisterResult", "�˺�ע��ɹ���");

        }
        //��¼
        private ServerPlayer Login(NetPlayer unClient, string acc, string pass)
        {
            //����˺Ŵ��ڲ��ܵ�½
            if (DataBase.Users.ContainsKey(acc))
            {
                ServerPlayer player = DataBase.Users[acc];
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
        public void CreateRoom(string roomName, string targetScene, int sceneCapacity)
        {
            RoomOperationCode callbackCode = new RoomOperationCode();
            if (roomName==string.Empty)
            {
                callbackCode.callBack = false;
                callbackCode.info = "����������Ϊ��";
                Send(RequestPlayer, "CreateRoomResult", callbackCode);
            }
            else if (Scenes.ContainsKey(roomName))
            {
                callbackCode.callBack = false;
                callbackCode.info = "�����ķ����Ѿ�����";
                Send(RequestPlayer, "CreateRoomResult", callbackCode);
            }
            else
            {
                //Scenes.TryAdd(roomName, new NetScene(sceneCapacity) { players = new List<NetPlayer>() { RequestPlayer } });
                NetScene netScene = new NetScene();
                netScene.players.Add(RequestPlayer);
                RequestPlayer.Scene = netScene;
                //��ԭ�������Ƴ����
                Scenes[RequestPlayer.sceneID].players.Remove(RequestPlayer);
                netScene.sceneCapacity = sceneCapacity;
                netScene.sceneName = targetScene;
                Scenes.TryAdd(roomName, netScene);
                RequestPlayer.sceneID = roomName;              
                DebugLog($"�ͻ��ˣ�{RequestPlayer.acc}������:{roomName}:����");


                callbackCode.callBack = true;
                callbackCode.info = "��������ɹ�";
                callbackCode.targetScene = targetScene;
                callbackCode.roomName = roomName;

                Send(RequestPlayer, "CreateRoomResult", callbackCode);
            }

        }

        [Net.Share.Rpc]
        public void JoinRoom(string roomName)
        {
            RoomOperationCode callbackCode = new RoomOperationCode();
            if (!Scenes.ContainsKey(roomName))
            {
                callbackCode.callBack = false;
                callbackCode.info = "����ķżٲ����ڻ��Ѿ���ɢ!";
                Send(RequestPlayer, "JoinRoomResult", callbackCode);
            }
            else if (Scenes[roomName].sceneCapacity == Scenes[roomName].SceneNumber)
            {
                callbackCode.callBack = false;
                callbackCode.info = "��������!";
                Send(RequestPlayer, "JoinRoomResult", callbackCode);
            }
            else if (roomName == RequestPlayer.sceneID)
            {
                callbackCode.callBack = false;
                callbackCode.info = "���Ѿ��ڷ�����!";
                Send(RequestPlayer, "JoinRoomResult", callbackCode);
            }
            else
            {

                Scenes[roomName].players.Add(RequestPlayer);
                RequestPlayer.Scene = Scenes[roomName];
                //��ԭ�������Ƴ����
                Scenes[RequestPlayer.sceneID].players.Remove(RequestPlayer);
                RequestPlayer.sceneID = roomName;

                callbackCode.callBack = true;
                callbackCode.info = "��������ɹ�";
                callbackCode.targetScene = Scenes[roomName].sceneName;
                callbackCode.roomName = roomName;

                Send(RequestPlayer, "JoinRoomResult", callbackCode);
            }
        }

        [Net.Share.Rpc]
        public void ExitRoom()
        {
            Send(RequestPlayer, "ExitRoomResult");
            var scene = Scenes[RequestPlayer.sceneID];
            if (scene.players.Contains(RequestPlayer))
            {
                Scenes[DefaultScene].players.Add(RequestPlayer);
                RequestPlayer.Scene = Scenes[DefaultScene];
                //��ԭ�������Ƴ����
                scene.players.Remove(RequestPlayer);
                Multicast(scene.players, "RemovePlayer", RequestPlayer.acc);
                if (scene.players.Count <= 0)
                {
                    Scenes.TryRemove(RequestPlayer.sceneID, out NetScene netScene);
                    DebugLog(RequestPlayer.sceneID + ":�����ɢ");
                }
               
                RequestPlayer.sceneID = DefaultScene;
            }
        }
    }
}