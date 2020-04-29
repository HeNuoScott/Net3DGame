using UnityEngine.UI;
using UnityEngine;
using Network;
using PBCommon;
using PBLogin;

public class Panel_Login : MonoBehaviour
{
    public Text text;
    public InputField inputField_Acc;
    public InputField inputField_Pas;
    public Button button_Login;
    public Button button_Register;
    public GameObject waitTip;
    public GameObject panel_Register;

    private void Start()
    {
        ClientService.GetSingleton().On_TCP_Message += LoginCon_On_TCP_Message;

        button_Login.onClick.AddListener(OnClickLogin);
        button_Register.onClick.AddListener(() => 
        {
            this.gameObject.SetActive(false);
            panel_Register.SetActive(true);
        });
    }

    private void LoginCon_On_TCP_Message(MessageBuffer msg)
    {
        if (ServerToClientID.TcpResponseLogin == (ServerToClientID)msg.Id())
        {
            TcpResponseLogin _mes = ProtoTransfer.DeserializeProtoBuf3<TcpResponseLogin>(msg);
            if (_mes.Result)
            {
                Debug.Log("登录成功～～～" + _mes.Uid);
                ClientService.GetSingleton().token = _mes.Token;
                //场景
                ClearSceneData.LoadScene(GameConfig.mainScene);
            }
            else
            {
                text.text = _mes.Reason;
                waitTip.SetActive(false);
            }
        }
    }
    //账号登录
    private void OnClickLogin()
    {
        text.text = "";
        waitTip.SetActive(true);
        string acc = inputField_Acc.text;
        string pas = inputField_Pas.text;
        if (acc == "" || pas == "")
        {
            text.text = "账号密码请输入完整";
            waitTip.SetActive(false);
            return;
        }
        TcpLogin _loginInfo = new TcpLogin
        {
            Account = acc,
            Password = pas,
            Token = ""
        };
        byte[] bytes = ProtoTransfer.SerializeProtoBuf3<TcpLogin>(_loginInfo);
        MessageBuffer message = new MessageBuffer((int)ClientToServerID.TcpLogin, bytes, 0);
        Network.ClientService.GetSingleton().SendTcp(message);
    }

    private void OnDisable()
    {
        text.text = "";
    }

    private void OnDestroy()
    {
        ClientService.GetSingleton().On_TCP_Message -= LoginCon_On_TCP_Message;
    }
}
