using UnityEngine.UI;
using UnityEngine;
using Network;
using PBCommon;
using PBLogin;


public class Panel_Connect : MonoBehaviour
{
    public Mode mode;
    public Protocol protocol;
    public Button button_Connect;
    public Dropdown dropdown_Mode;
    public Dropdown dropdown_Proto;
    public InputField inputField;
    public Text text;
    public GameObject waitTip;
    public GameObject panel_Login;

    private void Start()
    {
        waitTip.SetActive(false);
        ClientService.GetSingleton().On_TCP_Connect += LoginCon_On_TCP_Connect;
        ClientService.GetSingleton().On_TCP_Disconnect += LoginCon_On_TCP_Disconnect;

        button_Connect.onClick.AddListener(OnconnectServer);
        dropdown_Mode.onValueChanged.AddListener((index) => { mode = (Mode)index; });
        dropdown_Proto.onValueChanged.AddListener((index) => { protocol = (Protocol)index; });

        inputField.text = PlayerPrefs.GetString("ServerIP", "192.168.1.1");
    }

    private void LoginCon_On_TCP_Connect()
    {
        Debug.Log("连接成功");
        panel_Login.SetActive(true);
        this.gameObject.SetActive(false);
        waitTip.SetActive(false);
    }

    private void LoginCon_On_TCP_Disconnect()
    {
        waitTip.SetActive(false);
        text.text = "连接失败";
    }

    // 连接服务器
    private void OnconnectServer()
    {
        text.text = "";
        waitTip.SetActive(true);
        string _ip = inputField.text;
        PlayerPrefs.SetString("ServerIP", _ip);
        // 连接服务器
        ClientService.GetSingleton().Connect(_ip, NetConfig.TCP_PORT, NetConfig.UDP_PORT, mode, protocol);
    }

    private void OnDestroy()
    {
        ClientService.GetSingleton().On_TCP_Connect -= LoginCon_On_TCP_Connect;
        ClientService.GetSingleton().On_TCP_Disconnect -= LoginCon_On_TCP_Disconnect;
    }
}
