//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace QFramework.HeNuoApp
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.UI;
    using QF.Extensions;
    
    public class ChatPanelData : QFramework.UIPanelData
    {
    }
    
    public partial class ChatPanel : QFramework.UIPanel
    {
        
        protected override void ProcessMsg(int eventId, QFramework.QMsg msg)
        {
            throw new System.NotImplementedException ();
        }
        
        protected override void OnInit(QFramework.IUIData uiData)
        {
            mData = uiData as ChatPanelData ?? new ChatPanelData();
            // please add init code here
            Client.ChatManager.ShowMassage += ChatManager_ShowMassage;
            Button_Send.onClick.AddListener(() =>
            {
                string msg = InputField_Msg.text.Trim();
                if (msg!=null && msg!="")
                {
                    Client.ChatManager.Instance.SendMsg(msg);
                    InputField_Msg.text = "";
                }
            });
        }

        private void ChatManager_ShowMassage(string msg)
        {
            Content.text += msg;
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
            Client.ChatManager.ShowMassage -= ChatManager_ShowMassage;
        }
    }
}
