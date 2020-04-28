using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class User
    {
        int mID;
        string mToken;
        Session mClient;
        UserState mUserState;
        UserState mUserBattleState;
        UserAccountData mAccountData;
        public int Id { get { return mID; } }
        public string Token { get { return mToken; } }
        public Session Client { get { return mClient; } set { mClient = value; } }
        public UserState UserState { get { return mUserState; } set { mUserState = value; } }
        public UserState UserBattleState { get { return mUserBattleState; } set { mUserBattleState = value; } }
        public UserAccountData AccountData { get { return mAccountData; } set { mAccountData = value; } }

        public User(int id,string token, Session session, UserAccountData userAccount)
        {
            mID = id;
            mToken = token;
            mClient = session;
            mAccountData = userAccount;
            mUserState = UserState.OffLine;
            mUserBattleState = UserState.Lobbying;
        }

        public void Disconnect()
        {
            mClient.Disconnect();
        }
    }
}
