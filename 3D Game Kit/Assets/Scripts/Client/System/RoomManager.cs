using Net.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    /// <summary>
    /// ���������
    /// </summary>
    public class RoomManager : NetBehaviour
    {
        protected static RoomManager mInstance = null;
        public static RoomManager Instance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = GameObject.FindObjectOfType(typeof(RoomManager)) as RoomManager;
                    if (mInstance == null)
                    {
                        mInstance = new GameObject("_ " + typeof(RoomManager).ToString(), typeof(RoomManager)).GetComponent<RoomManager>();
                        DontDestroyOnLoad(mInstance);
                        mInstance.transform.SetParent(ClientNetworkManager.Instance.transform);
                    }
                }
                return mInstance;
            }
        }
        private void OnDestroy()
        {
            //�Ƴ�Զ�̵��ú���
            RemoveRpcDelegate(this);
            mInstance = null;
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