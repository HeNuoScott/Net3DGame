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
            RemoveRpcDelegate(this);
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
                //�������UDP��������  �߳�����1�벢����������
                while (!isResponse)
                {
                    Send(NetCmd.SceneCmd, "ChatMsg", DateTime.Now.ToString(),ClientNetworkManager.Instance.playerName, msg);
                    Thread.Sleep(1000);
                    times++;
                    if (times > outTime)
                    {
                        isResponse = true;
                        loginfo = "����ʧ��!" + msg;
                    }
                }
            });

        }

        /// <summary>
        /// ������Ϣ
        /// </summary>s
        [Net.Share.Rpc]private void ChatMsg(string time, string playerName,string info)
        {
            isResponse = true;
            string information = string.Empty;
            if (playerName == ClientNetworkManager.Instance.playerName)
            {
                information = string.Format("[{0}]\n{1}:{2}\n", time, playerName.ToMarkup(Markup.Green, Markup.Blod, Markup.Size(25)), info);
            }
            else information = string.Format("[{0}]\n{1}:{2}\n", time, playerName.ToMarkup(Markup.Yellow, Markup.Blod, Markup.Size(25)), info);

            ShowMassage?.Invoke(information);
        }
    }
}