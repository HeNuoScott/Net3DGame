using Network;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PBMatch;
using PBCommon;

public class MatchOrCancel : MonoBehaviour
{

    public Button button_Match;
    public Button button_Cancel;
    public GameObject ImageWait;

    private void Start()
    {
        ClientService.GetSingleton().On_TCP_Message += On_TCP_Message;

        button_Match.onClick.AddListener(OnClickMatch);
        button_Cancel.onClick.AddListener(OnClickCancel);
    }

    private void OnClickCancel()
    {
        byte[] bytes = ProtoTransfer.SerializeProtoBuf3<TcpCancelMatch>(new TcpCancelMatch() { Token = ClientService.GetSingleton().token });
        MessageBuffer message = new MessageBuffer((int)ClientToServerID.TcpCancelMatch, bytes, 0);
        ClientService.GetSingleton().SendTcp(message);
    }

    private void OnClickMatch()
    {
        byte[] bytes = ProtoTransfer.SerializeProtoBuf3<TcpRequestMatch>(new TcpRequestMatch() {Token = ClientService.GetSingleton().token });
        MessageBuffer message = new MessageBuffer((int)ClientToServerID.TcpRequestMatch, bytes, 0);
        ClientService.GetSingleton().SendTcp(message);
    }

    private void On_TCP_Message(MessageBuffer msg)
    {
        Debug.Log((ServerToClientID)msg.Id());

        // 请求匹配成功
        if (ServerToClientID.TcpResponseRequestMatch == (ServerToClientID)msg.Id())
        {
            ImageWait.SetActive(true);
        }
        // 取消匹配成功
        else if (ServerToClientID.TcpResponseCancelMatch == (ServerToClientID)msg.Id())
        {
            ImageWait.SetActive(false);
        }
        // 允许进入战场
        else if (ServerToClientID.TcpEnterBattle == (ServerToClientID)msg.Id())
        {
            Debug.Log("进入战场");
        }
    }

    private void OnDestroy()
    {
        ClientService.GetSingleton().On_TCP_Message -= On_TCP_Message;
    }
}
