using UnityEngine;
using QFramework;
using QF.Res;
using System.Net;
using System.Net.Sockets;
using System;

namespace QFramework.HeNuoApp
{
    public enum AppType
    {
        Server,
        Client
    }

    public class GameMain : MonoBehaviour
    {
        public AppType appType;
        public string IPAdress = "127.0.0.1";
        public int port = 666;
        public string DefaultScene { get; set; } = "MainScene";
        
        void Start()
        {
            ResKit.Init();
            UIMgr.SetResolution(1920, 1080, 0);

            switch (appType)
            {
                case AppType.Server:
                    UIMgr.OpenPanel<ServerPanel>(UILevel.Common);
                    break;
                case AppType.Client:
                    UIMgr.OpenPanel<ClientPanel>(UILevel.Common,new ClientPanelData()
                    {
                        defaultScene = this.DefaultScene,
                        ip = IPAdress,
                        port = this.port
                    });
                    UIMgr.OpenPanel<NetStatePanel>(UILevel.Forward);
                    break;
                default:
                    break;
            }
        }
    }
}