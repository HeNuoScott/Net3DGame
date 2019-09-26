using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using Net.Client;
using System.Net;
using System;
using QF;
using Net.Share;

namespace Client
{
    [MonoSingletonPath("[GameDesigner]/ClientNetworkManager")]
    public class ClientNetworkManager : MonoSingleton<ClientNetworkManager>
    {
        public string currentSceneName;
        public string currentRoomName;
        public string playerName;
        public string Acc;

        public Net.Client.UdpClient client = new Net.Client.UdpClient();
        public static event Action<Dictionary<string, RoomInfo>> UndataNetScenes;

        public Queue<string> queueLogInfo = new Queue<string>();

        private void Start()
        {

            //添加网络转换类型
            client.AddNetType<ChatMsg>();
            client.AddNetType<RoomOperationCode>();
            client.AddNetType<RoomInfo>();
            client.AddNetType<ServerPlayer>();
            client.AddNetType<Dictionary<string, RoomInfo>>();


            client.BindRpc(this);
        }

        public void Connect()
        {
            client.UseUnityThread = true;
            client.Log += (str) => DebugConnectLog(str);
            client.LogBug += (str) => Debug.Log("RPC远程调用失败" + str);
            //IPAddress[] ips = Dns.GetHostAddresses("www.LocalHost.com");
            //Client.host = ips[0].ToString();
            //string ip = NetworkManager.GetLocalIP();
            client.Connect(result => { if (result) client.Send(new byte[0]); });
        }

        public void Connect(string inputIP,int inputPort)
        {
            client.UseUnityThread = true;
            client.Log += (str) => DebugConnectLog(str);
            client.LogBug += (str) => Debug.Log("RPC远程调用失败" + str);

            //IPAddress[] ips = Dns.GetHostAddresses("www.LocalHost.com");
            //Client.host = ips[0].ToString();
            //string ip = NetworkManager.GetLocalIP();
            client.Connect(inputIP, inputPort, result => { if (result) client.Send(new byte[0]); });
        }

        public void DebugConnectLog(string str)
        {
            Debug.Log(str);
            queueLogInfo.Enqueue(str);
        }

        private void Update()
        {
            //unity线程运行客户端逻辑
            client.FixedUpdate();

            if (queueLogInfo.Count > 0)
            {
                string msg = queueLogInfo.Dequeue();
                NetMassageManager.OpenTitleMessage(msg);
            }
        }

        protected override void OnDestroy()
        {
            client.Close();
        }

        [Net.Share.Rpc]//获取用户信息结果
        private void CetUserSelfInfoResult(string playerName, string acc, string roomName, string sceneName)
        {
            this.currentRoomName = roomName;
            this.currentSceneName = sceneName;
            this.playerName = playerName;
            this.Acc = acc;
        }

        [Net.Share.Rpc]//其他地方登陆通知
        private void OtherLogin()
        {
            NetMassageManager.OpenMessage("您的账号在其他地方登录，请注意密码安全！");
        }
        [Net.Share.Rpc]//在大厅时接收所有房间信息
        private void OnLobbayReceiveScenesInfo(Dictionary<string, RoomInfo> scenes)
        {
            //UndataNetScenes?.Invoke(scenes);
            if (UndataNetScenes != null)
            {
                UndataNetScenes(scenes);
            }
        }

    }
}