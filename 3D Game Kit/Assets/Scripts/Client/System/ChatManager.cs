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
    /// ���������
    /// </summary>
    [MonoSingletonPath("[GameDesigner]/ChatManager")]
    public class ChatManager : NetClientMonoSingleton<ChatManager>
    {
        /// <summary>
        /// ��ʾ��Ϣ�¼�
        /// </summary>
        public static event Action<string> ShowMassage;
        //�Ƿ���Ӧ
        private bool isResponse = false;
        //��Ӧ��ʱ
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
            //�Ƴ�Զ�̵��ú���
            RemoveRpc(this);
        }

        /// <summary>
        /// ������Ϣ��ͬһ�����е�������
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
                //�������UDP��������  �߳�����1�벢����������
                while (!isResponse)
                {
                    Send(NetCmd.SceneCmd, "ChatMsg", chatMsg);
                    Thread.Sleep(1000);
                    times++;
                    if (times > outTime)
                    {
                        isResponse = true;
                        loginfo = "����ʧ��!" + msg+"\n";
                    }
                }
            });

        }

        /// <summary>
        /// ������Ϣ
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