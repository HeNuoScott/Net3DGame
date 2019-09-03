using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using Net.Client;
using System.Net;
using System;
using Net.Server;

namespace Client
{
    public class ClientNetworkManager : QF.MonoSingleton<ClientNetworkManager>
    {
        public string playerName;
        public string Acc;
        public Net.Client.UdpClient client = new Net.Client.UdpClient();
        public List<NetScene> netScenes = new List<NetScene>();
        public static event Action<List<NetScene>> UndataNetScenes;

        private string logInfo = string.Empty;

        private void Start()
        {
            client.BindRpc(this);
            //添加网络转换类型
            client.AddNetType<NetScene>();
            client.AddNetType<List<NetScene>>();
        }

        public void Connect()
        {
            client.UseUnityThread = true;
            client.Log += (str) =>
            {
                Debug.Log(str);
                logInfo = str;
            };
            client.LogBug += (str) =>
            {
                Debug.Log("RPC远程调用失败" + str);
            };
            //IPAddress[] ips = Dns.GetHostAddresses("www.LocalHost.com");
            //Client.host = ips[0].ToString();
            //string ip = NetworkManager.GetLocalIP();
            client.Connect(result => { if (result) client.Send(new byte[0]); });
        }

        public void Connect(string inputIP,int inputPort)
        {
            client.Log += (str) =>
            {
                Debug.Log(str);
                logInfo = str;
            };
            client.LogBug += (str) =>
            {
                Debug.Log("RPC远程调用失败" + str);
            };
            //IPAddress[] ips = Dns.GetHostAddresses("www.LocalHost.com");
            //Client.host = ips[0].ToString();
            //string ip = NetworkManager.GetLocalIP();
            client.Connect(inputIP, inputPort, result => { if (result) client.Send(new byte[0]); });
        }

        private void Update()
        {
            client.FixedUpdate();

            if (logInfo != string.Empty)
            {
                NetMassageManager.OpenTitleMessage(logInfo);
                logInfo = string.Empty;
            }
        }

        protected override void OnDestroy()
        {
            client.Close();
        }

        [Net.Share.Rpc]//获取用户信息结果
        private void CetUserSelfInfoResult(string playerName, string acc)
        {
            ClientNetworkManager.Instance.playerName = playerName;
            ClientNetworkManager.Instance.Acc = acc;
        }
        [Net.Share.Rpc]//其他地方登陆通知
        private void OtherLogin()
        {
            NetMassageManager.OpenMessage("您的账号在其他地方登录，请注意密码安全！");
        }
        [Net.Share.Rpc]//在大厅时接收所有房间信息
        private void OnLobbayReceiveScenesInfo(List<NetScene> scenes)
        {
            netScenes = scenes;
            UndataNetScenes?.Invoke(netScenes);
        }


    }
}