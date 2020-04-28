using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    /// <summary>
    /// 用户管理器
    /// </summary>
    public class UserManager
    {
        private static UserManager instance = null;
        public static UserManager Instance { get { return instance; } }
        public UserManager()
        {
            instance = this;
        }

        private int numberOfUser = 0;
        // toekn-->User
        private Dictionary<string, User> AllUser = new Dictionary<string, User>();

        /// <summary>
        /// Token是否有效
        /// </summary>
        public bool TokenIsValid(string token)
        {
            return AllUser.ContainsKey(token);
        }
        /// <summary>
        /// 根据token查找用户
        /// </summary>
        public User GetUserByToken(string token)
        {
            if (TokenIsValid(token) == false) return null;
            return AllUser[token];
        }
        /// <summary>
        /// 根据id查找用户
        /// </summary>
        public User GetUserByUid(int id)
        {
            User user = null;
            bool isRT = false;
            foreach (var item in AllUser)
            {
                if (!isRT&&item.Value.Id==id)
                {
                    user = item.Value;
                    isRT = true;
                }
            }
            return user;
        }

        /// <summary>
        /// 添加用户
        /// </summary>
        public void AddUser(string token, Session session,UserAccountData accountData)
        {
            numberOfUser++;
            if (TokenIsValid(token) == false)
            {
                AllUser.Add(token, new User(numberOfUser, token, session, accountData));
            }
        }
        /// <summary>
        /// 删除用户
        /// </summary>
        public void RemoveUser(string token)
        {
            if (TokenIsValid(token) == true)
            {
                AllUser.Remove(token);
            }
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        public void UserLogin(string token)
        {
            AllUser[token].UserState = UserState.Lobbying;
        }
        /// <summary>
        /// 用户退出
        /// </summary>
        public void UserLogOff(string token)
        {
            AllUser[token].UserState = UserState.OffLine;
        }
    }
}
