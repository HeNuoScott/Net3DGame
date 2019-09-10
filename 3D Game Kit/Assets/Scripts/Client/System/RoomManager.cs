using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using Net.Share;
using QF;
using System;

namespace Client
{
    /// <summary>
    /// 房间管理器
    /// </summary>
    [MonoSingletonPath("[GameDesigner]/RoomManager")]
    public class RoomManager : NetClientMonoSingleton<RoomManager>
    {
        public event Action<RoomOperationCode> CreateRoomCallback;
        public event Action<RoomOperationCode> JoinRoomRoomCallback;
        public event Action ExitRoomCallback;

        //是否响应
        private bool isResponse = false;
        //响应超时
        private int outTime = 5;
        public string loginfo = string.Empty;

        private void Update()
        {
            if (loginfo != string.Empty)
            {
                NetMassageManager.OpenTitleMessage(loginfo.ToMarkup(Color.red));
                loginfo = string.Empty;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            //移除远程调用函数
            RemoveRpcDelegate(this);
        }

        //发送创建房间的请求
        public void CreateRoom(string roomName, int number, string targetScene)
        {
            Send("CreateRoom", roomName, targetScene, number);
        }

        public void JoinRoom(string roomName)
        {
            Send("JoinRoom", roomName);
        }

        public void ExitRoom()
        {            
            isResponse = false;
            int times = 0;
            Task.Run(() =>
            {
                //如果出现UDP丢包现象  线程休眠1秒并反馈服务器
                while (!isResponse)
                {
                    Send("ExitRoom");
                    Thread.Sleep(1000);
                    times++;
                    if (times > outTime)
                    {
                        isResponse = true;
                        loginfo = "网络不佳,退出房间失败!";
                    }
                }
            });
        }

        [Net.Share.Rpc]
        private void CreateRoomResult(RoomOperationCode callbackCode)
        {
            if (callbackCode.callBack)
            {
                ClientNetworkManager.Instance.currentRoomName = callbackCode.roomName;
                //创建房间反馈
                CreateRoomCallback?.Invoke(callbackCode);
            }
            else
            {
                //创建房间失败
                NetMassageManager.OpenMessage(callbackCode.info);
            }
        }

        [Net.Share.Rpc]
        private void JoinRoomResult(RoomOperationCode callbackCode)
        {
            if (callbackCode.callBack)
            {
                ClientNetworkManager.Instance.currentRoomName = callbackCode.roomName;
                //加入房间成功
                JoinRoomRoomCallback?.Invoke(callbackCode);
            }
            else
            {
                //加入房间失败
                NetMassageManager.OpenMessage(callbackCode.info);
            }
        }

        [Net.Share.Rpc]
        private void ExitRoomResult()
        {
            isResponse = true;
            ClientNetworkManager.Instance.currentRoomName = "Lobby";
            ExitRoomCallback?.Invoke();
            //退出房间成功
        }

    }
}