using UnityEngine;
using Net.Client;
using QF;

namespace Client
{
    /// <summary>
    /// 登录和注册类
    /// </summary>
    [MonoSingletonPath("[GameDesigner]/LoginManager")]
    public class LoginManager : NetClientMonoSingleton<LoginManager>
    {
        public event System.Action LoginSucceedCallBack;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            //移除远程调用函数
            RemoveRpc(this);
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