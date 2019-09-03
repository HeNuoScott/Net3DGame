namespace Net.Server
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// 服务器数据库
    /// </summary>
    public class ServerDataBase
    {
        /// <summary>
        /// 所有玩家信息
        /// </summary>
        public static ConcurrentDictionary<string, NetPlayer> PlayerInfos = new ConcurrentDictionary<string, NetPlayer>();

        /// <summary>
        /// 获得所有玩家帐号数据
        /// </summary>
        public static List<T> Players<T>() where T : class
        {
            List<T> ts = new List<T>();
            foreach (var v in PlayerInfos.Values)
            {
                ts.Add(v as T);
            }
            return ts;
        }

        /// <summary>
        /// 加载数据库信息
        /// </summary>
        public static Task Load()
        {
            return LoadAsync();
        }

        /// <summary>
        /// 异步加载数据库信息
        /// </summary>
        public static Task LoadAsync()
        {
            return Task.Run(() =>
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                if (!Directory.Exists(baseDirectory + "/Data"))
                    Directory.CreateDirectory(baseDirectory + "/Data");
                string[] playerDataPaths = Directory.GetDirectories(baseDirectory + "Data");
                foreach (var path in playerDataPaths)
                {
                    try
                    {
                        FileStream fileStream = new FileStream(path + "/PlayerInfo.data", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        var buffer = new byte[1024*1024*10];
                        int count = fileStream.Read(buffer, 0, buffer.Length);
                        fileStream.Close();
                        var func = Share.NetConvert.Deserialize(buffer, 0, count);
                        if (func.pars.Length <= 0)
                            continue;
                        if (func.pars[0] is NetPlayer player)
                            PlayerInfos.TryAdd(player.playerID, player);
                    }
                    catch (Exception e) { Console.WriteLine(e); }
                }
            });
        }

        /// <summary>
        /// 存储全部玩家数据到文件里
        /// </summary>
        public static Task SaveAll()
        {
            return Task.Run(() =>
            {
                foreach (var p in PlayerInfos.Values)
                {
                    Save(p).Wait();
                }
            });
        }

        /// <summary>
        /// 存储单个玩家的数据到文件里
        /// </summary>
        public static Task Save(NetPlayer player)
        {
            if (player.playerID == string.Empty)
                throw new Exception("NetPlayer的playerID字段必须赋值，playerID就是Account的基值");
            return Task.Run(() =>
            {
                string path = AppDomain.CurrentDomain.BaseDirectory;
                if (!Directory.Exists(path + "Data"))
                    Directory.CreateDirectory(path + "Data");
                if (!Directory.Exists(path + "Data/" + player.playerID))
                    Directory.CreateDirectory(path + "Data/" + player.playerID);
                string path1 = path + "Data/" + player.playerID + "/PlayerInfo.data";
                FileStream fileStream;
                if (!File.Exists(path1))
                    fileStream = new FileStream(path1, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                else
                    fileStream = new FileStream(path1, FileMode.Truncate, FileAccess.ReadWrite, FileShare.ReadWrite);
                var bytes = Share.NetConvert.Serialize(player);
                fileStream.Write(bytes, 0, bytes.Length);
                fileStream.Close();
            });
        }
    }
}