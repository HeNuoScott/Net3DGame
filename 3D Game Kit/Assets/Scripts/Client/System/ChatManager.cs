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
            RemoveRpcDelegate(this);
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
                //如果出现UDP丢包现象  线程休眠1秒并反馈服务器
                while (!isResponse)
                {
                    Send(NetCmd.SceneCmd, "ChatMsg", DateTime.Now.ToString(),ClientNetworkManager.Instance.playerName, msg);
                    Thread.Sleep(1000);
                    times++;
                    if (times > outTime)
                    {
                        isResponse = true;
                        loginfo = "发送失败!" + msg;
                    }
                }
            });

        }

        /// <summary>
        /// 接收消息
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