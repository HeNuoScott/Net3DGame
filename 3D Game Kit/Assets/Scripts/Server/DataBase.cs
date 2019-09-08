using System.Collections.Generic;
using UnityEngine;
using Net.Server;
using Net.Share;

namespace Server
{
    //���ݿ�  �����û���Ϣ
    public class DataBase : Net.Server.ServerDataBase
    {
        /// <summary>
        /// ����ʱʹ�����  ���ø��������ConcurrentDictionary<string, NetPlayer> PlayerInfos
        /// </summary>
        public static Dictionary<string, ServerPlayer> Users = new Dictionary<string, ServerPlayer>();

        ///// <summary>
        ///// ��ʼ�����ݿ�
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
        //        NetGameServer.DebugLog("���ݿ��ʼ�����");
        //    });
        //}

        /// <summary>
        /// ����µ��˺���Ϣ
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
