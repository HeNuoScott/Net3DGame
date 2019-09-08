using Net.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QF;

namespace Client
{
    /// <summary>
    /// 房间管理器
    /// </summary>
    [MonoSingletonPath("[GameDesigner]/RoomManager")]
    public class RoomManager : NetClientMonoSingleton<RoomManager>
    {
        protected override void OnDestroy()
        {
            base.OnDestroy();
            //移除远程调用函数
            RemoveRpcDelegate(this);
        }
        //发送创建房间的请求
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
                Debug.Log("创建房间成功");
                //跳转场景
            }
            else
            {
                Debug.Log("创建房间失败:" + info);
            }
        }

        [Net.Share.Rpc]
        private void JoinRoomResult(bool result, string info)
        {
            if (result)
            {
                Debug.Log("加入房间成功");
            }
            else
            {
                Debug.Log("加入房间失败:" + info);
            }
        }


    }
}