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
        // account-->User
        private Dictionary<string, User> AllUser = new Dictionary<string, User>();
        // token-->account
        private Dictionary<string, string> LoginUser = new Dictionary<string, string>();

        public bool IsValidAccount(string account)
        {
            return AllUser.ContainsKey(account);
        }
        public bool IsValidToken(string token)
        {
            return LoginUser.ContainsKey(token);
        }

        public User GetUserByAccount(string account)
        {
            if (IsValidAccount(account)) return AllUser[account];
            else return null; 
        }
        public User GetUserByToken(string token)
        {
            if (IsValidToken(token)) return GetUserByAccount(LoginUser[token]);
            else return null;
        }

        /// <summary>
        /// 添加用户
        /// </summary>
        public void AddUser(string account, Session session,UserAccountData accountData)
        {
            numberOfUser++;
            if (IsValidAccount(account) == false)
            {
                string token = Guid.NewGuid().ToString();
                AllUser.Add(account, new User(numberOfUser, token, session, accountData));
            }
        }
        /// <summary>
        /// 删除用户
        /// </summary>
        public void RemoveUser(string account)
        {
            if (IsValidAccount(account) == true)
            {
                AllUser.Remove(account);
            }
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        public void UserLogin(string account)
        {
            User user = AllUser[account];
            user.UserState = UserState.Lobbying;
            LoginUser.Add(user.Token, account);
        }
        /// <summary>
        /// 用户退出
        /// </summary>
        public void UserLogOff(string account)
        {
            User user = AllUser[account];
            user.UserState = UserState.OffLine;
            LoginUser.Remove(user.Token);
        }
    }
}
