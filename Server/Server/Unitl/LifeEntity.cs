using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    /// <summary>
    /// 生命体
    /// </summary>
    public class LifeEntity
    {
        /// <summary>
        /// 角色id
        /// </summary>
        public int roleid;
        /// <summary>
        /// 类型、阵营
        /// </summary>
        public int type = 0;
        /// <summary>
        /// 名称
        /// </summary>
        public string name;


        /// <summary>
        /// 最大血量
        /// </summary>
        public int maxBlood = 0;
        /// <summary>
        /// 当前血量
        /// </summary>
        public int nowBlood = 0;

        /// <summary>
        /// 基础移速
        /// </summary>
        public int moveSpeed = 0;
        /// <summary>
        /// 移速加成
        /// </summary>
        public int moveSpeedAddition = 0;
        /// <summary>
        /// 移速百分比
        /// </summary>
        public int moveSpeedPercent = 0;

        /// <summary>
        /// 基础攻速
        /// </summary>
        public int attackSpeed = 0;
        /// <summary>
        /// 攻速加成
        /// </summary>
        public int attackSpeedAddition = 0;
        /// <summary>
        /// 攻速百分比
        /// </summary>
        public int attackSpeedPercent = 0;
    }
}
