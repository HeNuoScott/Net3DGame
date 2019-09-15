
namespace QFramework.HeNuoApp
{
    using QF.Action;
    using Client;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.UI;
    
    
    public class ClientPanelData : QFramework.UIPanelData
    {
        public string defaultScene = "Lobby";
        public string ip = "127.0.0.1";
        public int port = 666;
    }
    
    public partial class ClientPanel : QFramework.UIPanel
    {
        
        protected override void ProcessMsg(int eventId, QFramework.QMsg msg)
        {
            throw new System.NotImplementedException ();
        }
        
        protected override void OnInit(QFramework.IUIData uiData)
        {
            mData = uiData as ClientPanelData ?? new ClientPanelData();
            // please add init code here
            Client.ClientNetworkManager.Instance.Connect(mData.ip,mData.port);
            //添加网络通讯组件
            LoginManager.Instance.LoginSucceedCallBack += LoginOrRegister_LoginSucceedCallBack;
            //登录
            Button_Login.onClick.AddListener(() =>
            {
                if (!Client.ClientNetworkManager.Instance.client.Connected)
                {
                    NetMassageManager.OpenMessage("与服务器断开，请检查网络！");
                    return;
                }

                string acc = InputField_Acc.text;
                string pass = InputField_Pass.text;
                Client.LoginManager.Instance.Login(acc, pass);

                Button_Login.interactable = false;
                //10秒内不能重读点击登录
                this.Delay(10, () => { Button_Login.interactable = true;});
            });

            //注册
            Button_Register.onClick.AddListener(() =>
            {
                if (!Client.ClientNetworkManager.Instance.client.Connected)
                {
                    NetMassageManager.OpenMessage("与服务器断开，请检查网络！");
                    return;
                }

                string acc = InputField_Acc.text;
                string pass = InputField_Pass.text;
                //注册
                Client.LoginManager.Instance.Register(acc, pass);
                Button_Register.interactable = false;
                this.Delay(10, () =>
                {
                    Button_Register.interactable = true;
                });
            });
        }

        private void LoginOrRegister_LoginSucceedCallBack()
        {
            //登陆成功之后切换大厅面板
            QF.LSM.LoadSceneManager.Instance.LoadSceneAsync(mData.defaultScene, "LobbyPanel", UILevel.Common);
        }

        protected override void OnOpen(QFramework.IUIData uiData)
        {
        }
        
        protected override void OnShow()
        {
        }
        
        protected override void OnHide()
        {
        }
        
        protected override void OnClose()
        {
            //添加网络通讯组件
            LoginManager.Instance.LoginSucceedCallBack -= LoginOrRegister_LoginSucceedCallBack;
        }
    }
}
