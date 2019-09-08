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

namespace QFramework.HeNuoApp
{
	public partial class ScenePanel : UIElement
	{
        private string mySceneName;

        private void Awake()
		{
            Join_Button.onClick.AddListener(() =>
            {
                RoomManager.Instance.JoinRoom(mySceneName);
            });
		}

		protected override void OnBeforeDestroy()
		{
		}

        public void Init(string sceneName, NetScene sceneInfo)
        {
            if (sceneName == "MainScene")
            {
                SceneName.text = mySceneName = "大厅";
                Join_Button.interactable = false;
            }
            else SceneName.text = mySceneName = sceneName;

            capacity.text = string.Format("房间人数:{0}/{1}", sceneInfo.SceneNumber, sceneInfo.sceneCapacity);
        }
        public void ChangeInfo(NetScene sceneInfo)
        {
            capacity.text = string.Format("房间人数:{0}/{1}", sceneInfo.SceneNumber, sceneInfo.sceneCapacity);
        }
    }
}