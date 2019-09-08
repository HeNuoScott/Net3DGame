using Net.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QF;

namespace Client
{
    /// <summary>
    /// ���������
    /// </summary>
    [MonoSingletonPath("[GameDesigner]/RoomManager")]
    public class RoomManager : NetClientMonoSingleton<RoomManager>
    {
        protected override void OnDestroy()
        {
            base.OnDestroy();
            //�Ƴ�Զ�̵��ú���
            RemoveRpcDelegate(this);
        }
        //���ʹ������������
        public void CreateRoom(string roomName,int number)
        {
            Send("CreateRoom", roomName, number);
        }

        public void JoinRoom(string roomName)
        {
            Send("JoinRoom", roomName);
        }

        public void ExitRoom()
        {
            Send("ExitRoom");
        }

        [Net.Share.Rpc]
        private void CreateRoomResult(bool result,string info)
        {
            if (result)
            {
                Debug.Log("��������ɹ�");
                //��ת����
            }
            else
            {
                Debug.Log("��������ʧ��:" + info);
            }
        }

        [Net.Share.Rpc]
        private void JoinRoomResult(bool result, string info)
        {
            if (result)
            {
                Debug.Log("���뷿��ɹ�");
            }
            else
            {
                Debug.Log("���뷿��ʧ��:" + info);
            }
        }


    }
}