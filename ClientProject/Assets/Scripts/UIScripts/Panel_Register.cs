using UnityEngine.UI;
using UnityEngine;
using Network;
using PBCommon;
using PBLogin;

public class Panel_Register : MonoBehaviour
{
    public Text text;
    public InputField inputField_Acc;
    public InputField inputField_Pas;
    public Button button_Login;
    public Button button_Register;
    public GameObject waitTip;
    public GameObject panel_Login;

    private void Start()
    {
        ClientService.GetSingleton().On_TCP_Message += LoginCon_On_TCP_Message;

        button_Register.onClick.AddListener(OnClickRegister);
        button_Login.onClick.AddListener(() =>
        {
            this.gameObject.SetActive(false);
            panel_Login.SetActive(true);
        });
    }

    private void LoginCon_On_TCP_Message(MessageBuffer msg)
    {
        if (ServerToClientID.TcpResponseRegister == (ServerToClientID)msg.Id())
        {
            TcpResponseRegister _mes = ProtoTransfer.DeserializeProtoBuf3<TcpResponseRegister>(msg);
            if (_mes.Result)
            {
                Debug.Log("注册成功～～～");
                ClientService.GetSingleton().token = _mes.Token;
                this.gameObject.SetActive(false);
                panel_Login.SetActive(true);
                waitTip.SetActive(false);
            }
            else
            {
                text.text = "注册失败";
                waitTip.SetActive(false);
            }
        }
    }

    //账号登录
    private void OnClickRegister()
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
        TcpRegister _RegisterInfo = new TcpRegister
        {
            Account = acc,
            Password = pas,
        };
        byte[] bytes = ProtoTransfer.SerializeProtoBuf3<TcpRegister>(_RegisterInfo);
        MessageBuffer message = new MessageBuffer((int)ClientToServerID.TcpRegister, bytes, 0);
        ClientService.GetSingleton().SendTcp(message);
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
