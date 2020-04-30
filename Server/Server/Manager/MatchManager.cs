using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    /// <summary>
    /// 匹配管理器
    /// </summary>
    public class MatchManager
    {
        private static MatchManager instance = null;
        public static MatchManager Instance { get { return instance; } }
        /// <summary>
        /// 构建匹配管理器
        /// </summary>
        /// <param name="num">几个人一组的对战</param>
        public MatchManager()
        {
            mPVPNUmber = ServerConfig.PVP_Number;
            instance = this;
        }

        private int mPVPNUmber;
        private List<User> list_matchQueue = new List<User>();

        /// <summary>
        /// 添加匹配者
        /// </summary>
        public void AddMatchUser(User user)
        {
            // 判断队列中是否有用户  避免重复添加
            bool result = false;
            for (int i = 0; i < list_matchQueue.Count; i++)
            {
                if (list_matchQueue[i].Id == user.Id)
                {
                    result = true;
                    break;
                }
            }
            if (result) return;
            // 添加到队列中
            list_matchQueue.Add(user);
            // 判断当前队列人数大于房间开启人数
            if (list_matchQueue.Count >= mPVPNUmber)
            {
                List<User> _matchSureUser = new List<User>();
                for (int i = 0; i < mPVPNUmber; i++)
                {
                    _matchSureUser.Add(list_matchQueue[0]);
                    list_matchQueue.RemoveAt(0);
                }

                //达到房间人数  开始一场战斗
                //BattleManage.Instance.BeginBattle(_matchSureUser);

            }
        }

        /// <summary>
        /// 取消匹配
        /// </summary>
        public bool CancleMatch(User user)
        {
            bool result = false;
            for (int i = 0; i < list_matchQueue.Count; i++)
            {
                if (list_matchQueue[i].Id == user.Id)
                {
                    list_matchQueue.RemoveAt(i);
                    result = true;
                    break;
                }
            }
            return result;
        }
    }
}
