/****************************************************************************
 * 2019.9 DESKTOP-JJEQCQA
 ****************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using Net.Server;
using Client;
using Net.Share;

namespace QFramework.HeNuoApp
{
	public partial class ScenePanel : UIElement
	{
        private string myRoomName;

        private void Awake()
		{
            Join_Button.onClick.AddListener(() =>
            {
                RoomManager.Instance.JoinRoom(myRoomName);
            });
		}

		protected override void OnBeforeDestroy()
		{
		}

        public void Init(string roomName, RoomInfo roomInfo)
        {
            SceneName.text = myRoomName = roomName;

            capacity.text = string.Format("房间人数:{0}/{1}", roomInfo.roomNumber, roomInfo.roomCapacity);
        }
        public void ChangeInfo(RoomInfo roomInfo)
        {
            capacity.text = string.Format("房间人数:{0}/{1}", roomInfo.roomNumber, roomInfo.roomCapacity);
        }
    }
}