using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Net.Client;
using Net.Share;
using UnityEngine.SceneManagement;
using System.Threading;
using System.Threading.Tasks;
using QF;

namespace Client
{
    /// <summary>
    /// 生成管理器
    /// </summary>
    [MonoSingletonPath("[GameDesigner]/SpwanerManager")]
    public class SpwanerManager : NetClientMonoSingleton<SpwanerManager>
    {
        public GameObject player;
        Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();

        private bool isResponse = false;
        private int outTime = 10;
        public string loginfo = string.Empty;

        private void Update()
        {
            if (loginfo != string.Empty)
            {
                NetMassageManager.OpenTitleMessage(loginfo);
                loginfo = string.Empty;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            //移除远程调用函数
            RemoveRpcDelegate(this);
        }

        public void SendCreatePlayerRequest()
        {
            isResponse = false;
            int times = 0;
            Task.Run(() =>
            {
                while (!isResponse)
                {
                    Send("CreatePlayer");
                    Thread.Sleep(1000);
                    times++;
                    if (times > outTime)
                    {
                        isResponse = true;
                        loginfo = "请求超时";
                        Debug.Log("请求超时");

                    }
                }
            });
        }

        [Net.Share.Rpc]
        public void CreateLocalPlayer(string playerName)
        {
            isResponse = true;
            GameObject palyer = Instantiate(this.player, transform.position, transform.rotation);
            player.GetComponent<Player>().playerName = playerName;
            ClientNetworkManager.Instance.playerName = playerName;
        }

        [Net.Share.Rpc]
        public void CreateOtherPlayer(string playerName)
        {
            if (playerName == ClientNetworkManager.Instance.playerName) return;
            if (players.ContainsKey(playerName)) return;
            GameObject palyer = Instantiate(this.player, transform.position, transform.rotation);
            player.GetComponent<Player>().playerName = playerName;

            players.Add(playerName, palyer);
        }

        [Net.Share.Rpc]
        public void RemovePlayer(string playerName)
        {
            if (players.ContainsKey(playerName))
            {
                Destroy(players[playerName]);
                players.Remove(playerName);
            }
        }

    }
}