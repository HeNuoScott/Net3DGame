using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class UserAccountData
    {
        public UserAccountData(string account,string password)
        {
            Account = account;
            Password = password;
        }
        public string Account { get; set; }
        public string Password { get; set; }
    }
}
