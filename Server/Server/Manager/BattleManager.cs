using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    /// <summary>
    /// 战斗管理器
    /// </summary>
    public class BattleManager
    {
        private static BattleManager instance = null;
        public static BattleManager Instance { get { return instance; } }
        public BattleManager()
        {
            instance = this;
        }

        private int battleID = 0;
        private Dictionary<int, BattleController> dic_battles = new Dictionary<int, BattleController>();
        /// <summary>
        /// 开始战斗
        /// </summary>
        /// <param name="_battleUser"></param>
        public void BeginBattle(List<User> _battleUser)
        {
            battleID++;
            BattleController _battle = new BattleController();
            _battle.CreatBattle(battleID, _battleUser);
            dic_battles[battleID] = _battle;
            Debug.Log("开始战斗。。。。。" + battleID);
        }

        /// <summary>
        /// 结束战斗
        /// </summary>
        public void FinishBattle(int _battleID)
        {
            dic_battles.Remove(_battleID);
            Debug.Log("战斗结束。。。。。" + _battleID);
        }
    }
}
