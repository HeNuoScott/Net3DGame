namespace Net.Server
{
    using Net.Share;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 网络场景(房间)
    /// </summary>
    public class NetScene
    {
        /// <summary>
        /// 游戏中场景的名称
        /// </summary>
        public string gameSceneName = "GameScene";

        /// <summary>
        /// 场景容纳人数
        /// </summary>
        public int sceneCapacity = 0;
        /// <summary>
        /// 当前网络场景的玩家
        /// </summary>
        public List<NetPlayer> players = new List<NetPlayer>();
        /// <summary>
        /// 当前网络场景状态
        /// </summary>
        public NetState state = NetState.Idle;

        /// <summary>
        /// 获取场景当前人数
        /// </summary>
        public int SceneNumber {
            get { return players.Count; }
        }

        /// <summary>
        /// 构造网络场景
        /// </summary>
        public NetScene() { }

        /// <summary>
        /// 添加网络主场景并增加主场景最大容纳人数
        /// </summary>
        /// <param name="number">主创建最大容纳人数</param>
        public NetScene(int number)
        {
            sceneCapacity = number;
        }

        /// <summary>
        /// 添加网络场景并增加当前场景人数
        /// </summary>
        /// <param name="client">网络玩家</param>
        /// <param name="number">创建场景容纳人数</param>
        public NetScene(NetPlayer client, int number)
        {
            players.Add(client);
            sceneCapacity = number;
        }

        /// <summary>
        /// 获得当前场景的所有玩家转类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> Players<T>() where T : class
        {
            List<T> ts = new List<T>();
            foreach (var v in players)
            {
                ts.Add(v as T);
            }
            return ts;
        }

        /// <summary>
        /// 转换对象集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[] Convert<T>(Array objs) where T : class
        {
            T[] array = new T[objs.Length];
            for (int i = 0; i < objs.Length; i++)
            {
                array[i] = objs.GetValue(i) as T;
            }
            return array;
        }
    }
}