namespace Net.Server
{
    using Net.Share;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 网络场景实体就等于房间
    /// </summary>
    public class ServerNetRoom : NetScene
    {
        /// <summary>
        /// 房间名称
        /// </summary>
        public string roomName;
        /// <summary>
        /// 场景名称
        /// </summary>
        public string sceneName;
        /// <summary>
        /// 预设生成位置集合
        /// </summary>
        private List<Vector3> SpwanerPointList;
        /// <summary>
        /// 玩家对应的 生成位置
        /// </summary>
        public Dictionary<string, Vector3> spwanerPos = new Dictionary<string, Vector3>();

        public ServerNetRoom(ServerPlayer player, int number,string RoomName, string SceneName)
        {
            sceneCapacity = number;
            roomName = RoomName;
            sceneName = SceneName;
            NetGameServer.Instance.Scenes.TryAdd(roomName, this);

            //从原场景中移除玩家
            player.Scene.players.Remove(player);
            //添加到新场景中
            players.Add(player);
            player.Scene = this;
            player.sceneID = roomName;

            SpwanerPointList = new List<Vector3>()
            {
                new Vector3(-1,0,1),
                new Vector3(0,0,1),
                new Vector3(1,0,1),

                new Vector3(-1,0,0),
                new Vector3(1,0,0),

                new Vector3(-1,0,-1),
                new Vector3(0,0,-1),
                new Vector3(1,0,-1),
            };
            player.spwanerPoint = FindSpwanerPoint();
            spwanerPos.Add(player.playerID, player.spwanerPoint);
        }
        public ServerNetRoom(int number, string RoomName, string SceneName)
        {
            sceneCapacity = number;
            roomName = RoomName;
            sceneName = SceneName;
        }

        public void JoinRoom(ServerPlayer player)
        {
            //从原场景中移除玩家
            player.Scene.players.Remove(player);
            //添加到新场景中
            players.Add(player);
            player.Scene = this;
            player.sceneID = roomName;

            if (roomName == "MainScene") return;

            player.spwanerPoint = FindSpwanerPoint();
            spwanerPos.Add(player.playerID, player.spwanerPoint);
        }
        /// <summary>
        /// 退回到大厅
        /// </summary>
        /// <param name="player"></param>
        public void ExitRoom(ServerPlayer player)
        {
            if (players.Contains(player)) players.Remove(player);

            if (roomName == "MainScene") return;

            //回收生成位置点
            Vector3 playerSPOS = spwanerPos[player.playerID];
            if (playerSPOS != Vector3.zero)
                SpwanerPointList.Add(spwanerPos[player.playerID]);
            spwanerPos.Remove(player.playerID);

            NetGameServer.Instance.Multicast(players, "RemovePlayer", player.acc);
            ServerNetRoom defaultScene = NetGameServer.Instance.Scenes["MainScene"] as ServerNetRoom;
            if (!defaultScene.players.Contains(player))
            {
                defaultScene.JoinRoom(player);
            }

            if (players.Count <= 0)
            {
                NetGameServer.Instance.Scenes.TryRemove(roomName, out NetScene netScene);
                NetGameServer.DebugLog(roomName + ":房间解散");
            }

        }

        public Vector3 FindSpwanerPoint()
        {

            if (SpwanerPointList.Count==0) 
            {
                return new Vector3(0, 0, 0);
            }
            else
            {
                Vector3 spwanerPoint = SpwanerPointList[0];
                SpwanerPointList.RemoveAt(0);
                return spwanerPoint;
            }
        }
    }
}