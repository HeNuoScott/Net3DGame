using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using Net.Client;
using Net.Share;
using System;
using QF;

namespace Client
{
    /// <summary>
    /// 聊天管理器
    /// </summary>
    [MonoSingletonPath("[GameDesigner]/ChatManager")]
    public class ChatManager : NetClientMonoSingleton<ChatManager>
    {
        /// <summary>
        /// 显示消息事件
        /// </summary>
        public static event Action<string> ShowMassage;
        //是否响应
        private bool isResponse = false;
        //响应超时
        private int outTime = 5;
        public string loginfo = string.Empty;

        public ChatMsg receiveChatMsg = new ChatMsg();

        private void Update()
        {
            if (loginfo != string.Empty)
            {
                ShowMassage?.Invoke(loginfo.ToMarkup(Color.red));
                loginfo = string.Empty;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            //移除远程调用函数
            RemoveRpc(this);
        }

        /// <summary>
        /// 发送消息给同一场景中的所有人
        /// </summary>
        /// <param name="msg"></param>
        public void SendMsg(string msg)
        {
            isResponse = false;
            int times = 0;
            Task.Run(() =>
            {
                ChatMsg chatMsg = new ChatMsg()
                {
                    msgSender = ClientNetworkManager.Instance.playerName,
                    msgTime = DateTime.Now.ToString(),
                    msgInfo = msg
                };
                //如果出现UDP丢包现象  线程休眠1秒并反馈服务器
                while (!isResponse)
                {
                    Send(NetCmd.SceneCmd, "ChatMsg", chatMsg);
                    Thread.Sleep(1000);
                    times++;
                    if (times > outTime)
                    {
                        isResponse = true;
                        loginfo = "发送失败!" + msg+"\n";
                    }
                }
            });

        }

        /// <summary>
        /// 接收消息
        /// </summary>s
        [Net.Share.Rpc]private void ChatMsg(ChatMsg msg)
        {
            isResponse = true;

            if (receiveChatMsg == msg) return;

            receiveChatMsg = msg;

            if (receiveChatMsg.msgSender == ClientNetworkManager.Instance.playerName)
            {
                string infoSelf = string.Format("[{0}]\n{1}:{2}\n", receiveChatMsg.msgTime, 
                    receiveChatMsg.msgSender.ToMarkup(Markup.Green, Markup.Blod, Markup.Size(25)),receiveChatMsg.msgInfo);

                ShowMassage?.Invoke(infoSelf);
            }
            else
            {
                string infoOther = string.Format("[{0}]\n{1}:{2}\n", receiveChatMsg.msgTime,
                    receiveChatMsg.msgSender.ToMarkup(Markup.Green, Markup.Blod, Markup.Size(25)), receiveChatMsg.msgInfo);

                ShowMassage?.Invoke(infoOther);
            }
        }
    }
}