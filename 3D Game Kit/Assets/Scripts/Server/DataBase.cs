using System.Collections.Generic;
using UnityEngine;
using Net.Server;
using Net.Share;

namespace Net.Server
{
    //数据库  储存用户信息
    public class DataBase : ServerDataBase
    {
        /// <summary>
        /// 运行时使用这个  不用父类里面的ConcurrentDictionary<string, NetPlayer> PlayerInfos
        /// </summary>
        public static Dictionary<string, ServerPlayer> Users = new Dictionary<string, ServerPlayer>();

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
        public static void Add(ServerPlayer player)
        {
            Users.Add(player.acc, player);
            //Save(player);
        }
    }
}
