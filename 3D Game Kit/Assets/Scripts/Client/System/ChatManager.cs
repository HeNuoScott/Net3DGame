using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using Net.Client;
using Net.Share;
using System;

namespace Client
{
    /// <summary>
    /// ���������
    /// </summary>
    public class ChatManager : NetBehaviour
    {
        protected static ChatManager mInstance = null;
        public static ChatManager Instance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = GameObject.FindObjectOfType(typeof(ChatManager)) as ChatManager;
                    if (mInstance == null)
                    {
                        mInstance = new GameObject("_ " + typeof(ChatManager).ToString(), typeof(ChatManager)).GetComponent<ChatManager>();
                        DontDestroyOnLoad(mInstance);
                        mInstance.transform.SetParent(ClientNetworkManager.Instance.transform);
                    }
                }
                return mInstance;
            }
        }
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

        private void OnDestroy()
        {
            //�Ƴ�Զ�̵��ú���
            RemoveRpcDelegate(this);
            mInstance = null;
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