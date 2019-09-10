using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using Net.Share;
using QF;
using System;

namespace Client
{
    /// <summary>
    /// ���������
    /// </summary>
    [MonoSingletonPath("[GameDesigner]/RoomManager")]
    public class RoomManager : NetClientMonoSingleton<RoomManager>
    {
        public event Action<RoomOperationCode> CreateRoomCallback;
        public event Action<RoomOperationCode> JoinRoomRoomCallback;
        public event Action ExitRoomCallback;

        //�Ƿ���Ӧ
        private bool isResponse = false;
        //��Ӧ��ʱ
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
            //�Ƴ�Զ�̵��ú���
            RemoveRpcDelegate(this);
        }

        //���ʹ������������
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
                //�������UDP��������  �߳�����1�벢����������
                while (!isResponse)
                {
                    Send("ExitRoom");
                    Thread.Sleep(1000);
                    times++;
                    if (times > outTime)
                    {
                        isResponse = true;
                        loginfo = "���粻��,�˳�����ʧ��!";
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
                //�������䷴��
                CreateRoomCallback?.Invoke(callbackCode);
            }
            else
            {
                //��������ʧ��
                NetMassageManager.OpenMessage(callbackCode.info);
            }
        }

        [Net.Share.Rpc]
        private void JoinRoomResult(RoomOperationCode callbackCode)
        {
            if (callbackCode.callBack)
            {
                ClientNetworkManager.Instance.currentRoomName = callbackCode.roomName;
                //���뷿��ɹ�
                JoinRoomRoomCallback?.Invoke(callbackCode);
            }
            else
            {
                //���뷿��ʧ��
                NetMassageManager.OpenMessage(callbackCode.info);
            }
        }

        [Net.Share.Rpc]
        private void ExitRoomResult()
        {
            isResponse = true;
            ClientNetworkManager.Instance.currentRoomName = "Lobby";
            ExitRoomCallback?.Invoke();
            //�˳�����ɹ�
        }

    }
}