using System.Collections.Generic;
using UnityEngine;
using Net.Server;

namespace Server
{
    public class Player : NetPlayer
    {
        public string acc, pass;
        public Vector3 pos;
        public Quaternion roto;
    }
    //数据库  储存用户信息
    public class DataBase : Net.Server.ServerDataBase
    {
        /// <summary>
        /// 运行时使用这个  不用父类里面的ConcurrentDictionary<string, NetPlayer> PlayerInfos
        /// </summary>
        public static Dictionary<string, Player> Users = new Dictionary<string, Player>();

        ///// <summary>
        ///// 初始化数据库
        ///// </summary>
        //public static void DataBaseInit()
        //{
        //    LoadAsync(()=> 
        //    {
        //        List<Player> players = Players<Player>();
        //        foreach (var item in players)
        //        {
        //            Users.Add(item.acc, item);
        //        }
        //        NetGameServer.DebugLog("数据库初始化完成");
        //    });
        //}

        /// <summary>
        /// 添加新的账号信息
        /// </summary>
        /// <param name="playerName"></param>
        /// <param name="accountNumber"></param>
        /// <param name="password"></param>
        public static void Add(Player player)
        {
            Users.Add(player.acc, player);
            //Save(player);
        }
    }
}
