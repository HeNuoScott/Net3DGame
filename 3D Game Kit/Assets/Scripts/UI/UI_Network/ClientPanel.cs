
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
            //�������ͨѶ���
            LoginManager.Instance.LoginSucceedCallBack += LoginOrRegister_LoginSucceedCallBack;
            //��¼
            Button_Login.onClick.AddListener(() =>
            {
                if (!Client.ClientNetworkManager.Instance.client.Connected)
                {
                    NetMassageManager.OpenMessage("��������Ͽ����������磡");
                    return;
                }

                string acc = InputField_Acc.text;
                string pass = InputField_Pass.text;
                Client.LoginManager.Instance.Login(acc, pass);

                Button_Login.interactable = false;
                //10���ڲ����ض������¼
                this.Delay(10, () => { Button_Login.interactable = true;});
            });

            //ע��
            Button_Register.onClick.AddListener(() =>
            {
                if (!Client.ClientNetworkManager.Instance.client.Connected)
                {
                    NetMassageManager.OpenMessage("��������Ͽ����������磡");
                    return;
                }

                string acc = InputField_Acc.text;
                string pass = InputField_Pass.text;
                //ע��
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
            //��½�ɹ�֮���л��������
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
            //�������ͨѶ���
            LoginManager.Instance.LoginSucceedCallBack -= LoginOrRegister_LoginSucceedCallBack;
        }
    }
}
