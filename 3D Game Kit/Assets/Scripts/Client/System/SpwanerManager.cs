using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Net.Client;
using Net.Share;
using UnityEngine.SceneManagement;
using System.Threading;
using System.Threading.Tasks;
using QF;
using QF.Res;

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

        private ResLoader mResLoader;

        private void Start()
        {
            mResLoader = ResLoader.Allocate();
            player = mResLoader.LoadSync<GameObject>("player");
        }

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
            RemoveRpc(this);

            mResLoader.Recycle2Cache();
            mResLoader = null;
        }

        public void SendCreatePlayerRequest()
        {
            players.Clear();
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
        public void CreateLocalPlayer(string playerName,Vector3 spwanerPosition)
        {
            isResponse = true;
            GameObject localPalyer = Instantiate(this.player, spwanerPosition, transform.rotation);
            localPalyer.GetComponent<ClientPlayer>().playerName = playerName;
            ClientNetworkManager.Instance.playerName = playerName;

            players.Add(playerName, localPalyer);
        }

        [Net.Share.Rpc]
        public void CreateOtherPlayer(string playerName,Vector3 spwanerPosition)
        {
            if (playerName == ClientNetworkManager.Instance.playerName || players.ContainsKey(playerName)) return;

            GameObject palyerIns = Instantiate(this.player, spwanerPosition, transform.rotation);
            palyerIns.GetComponent<ClientPlayer>().playerName = playerName;

            players.Add(playerName, palyerIns);

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