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
    //���ݿ�  �����û���Ϣ
    public class DataBase : Net.Server.ServerDataBase
    {
        /// <summary>
        /// ����ʱʹ�����  ���ø��������ConcurrentDictionary<string, NetPlayer> PlayerInfos
        /// </summary>
        public static Dictionary<string, Player> Users = new Dictionary<string, Player>();

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
        public static void Add(Player player)
        {
            Users.Add(player.acc, player);
            //Save(player);
        }
    }
}
