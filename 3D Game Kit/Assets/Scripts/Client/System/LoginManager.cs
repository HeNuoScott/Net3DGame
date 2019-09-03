using UnityEngine;
using Net.Client;

namespace Client
{
    /// <summary>
    /// 登录和注册类
    /// </summary>
    public class LoginManager : NetBehaviour
    {
        public event System.Action LoginSucceedCallBack;
        protected static LoginManager mInstance = null;
        public static LoginManager Instance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = GameObject.FindObjectOfType(typeof(LoginManager)) as LoginManager;
                    if (mInstance == null)
                    {
                        mInstance = new GameObject("_ " + typeof(LoginManager).ToString(), typeof(LoginManager)).GetComponent<LoginManager>();
                        DontDestroyOnLoad(mInstance);
                        mInstance.transform.SetParent(ClientNetworkManager.Instance.transform);
                    }
                }
                return mInstance;
            }
        }
        private void OnDestroy()
        {
            //移除远程调用函数
            RemoveRpcDelegate(this);
            mInstance = null;
        }
        //注册
        public void Register(string acc, string pass)
        {
            if (acc.Trim() == string.Empty || pass.Trim() == string.Empty)
            {
                NetMassageManager.OpenMessage("注册失败：账号密码不能为空！");
                return;
            }
            Send("Register", acc, pass);
        }
        //登录
        public void Login(string acc, string pass)
        {
            if (acc.Trim() == string.Empty || pass.Trim() == string.Empty)
            {
                NetMassageManager.OpenMessage("登录失败：账号密码不能为空！");
                return;
            }
            Send("Login", acc, pass);
        }
        //获取用户信息
        public void CetUserSelfInfo()
        {
            Send("CetUserSelfInfo");
        }

        [Net.Share.Rpc]//登录结果
        private void LoginResult(bool result, string info)
        {
            //登陆成功
            if (result)
            {
                //登陆成功之后回调
                LoginSucceedCallBack?.Invoke();
                //获取用户信息
                CetUserSelfInfo();
            }
            else NetMassageManager.OpenMessage(info);
        }
        [Net.Share.Rpc]//注册结果
        private void RegisterResult(string result)
        {
            NetMassageManager.OpenMessage(result);
        }
    }
}