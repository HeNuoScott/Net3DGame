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
        /// 请求操作的玩家
        /// 服务器所有方法即是在多线程中完成   多线程不能直接访问unity组件的属性和方法
        /// </summary>
        private Player RequestPlayer;
        public int OnLineCount{get { return Players.Count; }}
        public static event Action<string> ServerLog;
        public static void DebugLog(string str) => ServerLog?.Invoke(str);

        protected override void OnStarting()
        {
            DebugLog("开始启动服务器");
            this.Log += (msg) => { DebugLog(msg); };
            //添加网络转换类型
            Net.Share.NetConvert.AddNetworkType<NetScene>();
            Net.Share.NetConvert.AddNetworkType<List<NetScene>>();
        }
        protected override void OnStartupCompleted()
        {
            DebugLog("服务器启动完成");      
        }
        protected override void OnHasConnect(NetPlayer client)
        {
            DebugLog($"客户端：{client.RemotePoint[GUID].ToString()}链接......");
        }
        protected override void OnRemoveClient(NetPlayer client)
        {
            DebugLog($"客户端：{client.playerID.ToString()}断开链接......");
        }
        protected override void OnReceiveBuffer(NetPlayer client, byte cmd, byte[] buffer, int index, int count)
        {
            //DebugLog("接收原生byte[]数据:");
            base.OnReceiveBuffer(client, cmd, buffer, index, count);
        }
        //用户调用客户端的Rpc函数时调用
        protected override void OnInvokeRpc(NetPlayer client)
        {
            RequestPlayer = client as Player;
        }
        //当用户发送请求时调用
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
            //如果登录失败返回null，客户端不添加到字典当中（消息发送不参与）
            return null;
        }
        //添加网络默认场景
        protected override KeyValuePair<string, NetScene> OnAddDefaultScene()
        {
            DebugLog("添加网络默认场景");
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

        //注册
        private void Register(NetPlayer unClient, string acc, string pass)
        {
            //如果账号存在，不能注册
            if (DataBase.Users.ContainsKey(acc))
            {
                Send(unClient, "RegisterResult", "账号已存在，注册失败！");
                return;
            }
            Player player = new Player();
            player.acc = acc;
            player.pass = pass;
            player.playerID = acc;//用账号标识玩家的唯一标识ID

            DataBase.Add(player);

            DebugLog($"客户端：{player.acc}账号注册成功！......");
            Send(unClient, "RegisterResult", "账号注册成功！");

        }
        //登录
        private Player Login(NetPlayer unClient, string acc, string pass)
        {
            //如果账号存在才能登陆
            if (DataBase.Users.ContainsKey(acc))
            {
                Player player = DataBase.Users[acc];
                if (player.pass == pass)
                {
                    //所有在线的客户端  玩家已经登录的状态
                    if (Players.ContainsKey(acc))
                    {
                        NetPlayer OnLinePlayer = Players[acc];
                        Send(OnLinePlayer, "OtherLogin");//提醒在线玩家
                        Send(unClient, "LoginResult", false, "账号已经登录");
                        return null;
                    }
                    DebugLog($"客户端：{player.acc}账号登陆成功！......");
                    //玩家未登录的状态
                    Send(unClient, "LoginResult", true, "登陆成功");
                    return player;
                }
            }
            Send(unClient, "LoginResult", false, "账号或密码有误，登录失败！");
            return null;
        }

        [Net.Share.Rpc]
        public void CreatePlayer()
        {
            //创建客户端自身玩家
            Send(RequestPlayer, "CreateLocalPlayer", RequestPlayer.acc);
            //通过玩家的ID找到所属房间,里面的所有玩家
            List<NetPlayer> players = Scenes[RequestPlayer.sceneID].players;
            //(广播)发送给其他客户端有人进入了 同一个场景 ,让客户端创建相应的 玩家对象
            Multicast(players, "CreateOtherPlayer", RequestPlayer.acc);
            //获取场景中所有的玩家,在自己的客户端也创建他们的玩家对象
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
                Send(RequestPlayer, "CreateRoomResult", false, "房间名不能为空");
            }
            else if (Scenes.ContainsKey(roomName))
            {
                Send(RequestPlayer, "CreateRoomResult", false, "创建的房间已经存在");
            }
            else
            {
                Scenes.TryAdd(roomName, new NetScene(sceneCapacity) { players = new List<NetPlayer>() { RequestPlayer } });
                RequestPlayer.sceneID = roomName;
                DebugLog($"客户端：{RequestPlayer.acc}创建了:{roomName}:房间");
                Send(RequestPlayer, "CreateRoomResult", true, "创建房间成功");
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
                DebugLog(RequestPlayer.sceneID+":房间解散");
            }
            RequestPlayer.sceneID = DefaultScene;
        }

        [Net.Share.Rpc]
        public void JoinRoom(string roomName)
        {
            if (!Scenes.ContainsKey(roomName))
            {
                Send(RequestPlayer, "JoinRoomResult", false, "加入的放假不存在或已经解散!");
            }
            else if (Scenes[roomName].sceneCapacity == Scenes[roomName].SceneNumber)
            {
                Send(RequestPlayer, "JoinRoomResult", false, "房间已满!");
            }
            else
            {
                Scenes[roomName].players.Add(RequestPlayer);
                RequestPlayer.sceneID = roomName;
                Send(RequestPlayer, "JoinRoomResult", true, "加入房间成功");
            }
        }



    }
}