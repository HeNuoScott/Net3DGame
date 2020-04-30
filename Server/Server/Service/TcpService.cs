using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System;
using PBMessage;

namespace Server
{
    /// <summary>
    /// TCP 服务器
    /// </summary>
    public class TcpService : TcpListener
    {
        private static TcpService instance = null;
        public static TcpService Instance { get { return instance; } }

        private readonly int mPort;
        private int numberOfClient = 1;
        private Thread mAcceptThread, mReceiveThread, mSendThread, mActiveThread;
        private Queue<MessageInfo> mSendMessageQueue = new Queue<MessageInfo>();
        public List<Session> mClientSessions = new List<Session>();
        public bool IsActive { get { return Active; } }

        public TcpService(int port) : base(IPAddress.Any, port)
        {
            instance = this;
            mPort = port;
        }

        public bool Listen()
        {
            if (IsActive) return true;
            // 开始侦听传入的连接请求。
            Start();

            mAcceptThread = new Thread(AcceptThread);
            mReceiveThread = new Thread(ReceiveThread);
            mSendThread = new Thread(SendThread);
            mActiveThread = new Thread(ActiveThreadHeart);

            mAcceptThread.Start();
            mReceiveThread.Start();
            mSendThread.Start();
            mActiveThread.Start();

            return true;
        }

        public void Close()
        {
            Stop();

            if (mAcceptThread != null)
            {
                mAcceptThread.Abort();
                mAcceptThread = null;
            }
            if (mReceiveThread != null)
            {
                mReceiveThread.Abort();
                mReceiveThread = null;
            }
            if (mSendThread != null)
            {
                mSendThread.Abort();
                mSendThread = null;
            }
            if (mActiveThread != null)
            {
                mActiveThread.Abort();
                mActiveThread = null;
            }
        }

        private void ActiveThreadHeart()
        {
            while (IsActive)
            {
                lock (mClientSessions)
                {
                    for (int i = 0; i < mClientSessions.Count; i++)
                    {
                        if (mClientSessions[i].IsConnected == false)
                        {
                            Debug.Log("断开连接：" + mClientSessions[i].Id);
                            mClientSessions[i].Disconnect();
                        }
                    }
                }
                Thread.Sleep(1000);
            } 
        }

        public void Send(MessageInfo message)
        {
            if (message == null) return;

            lock (mSendMessageQueue)
            {
                mSendMessageQueue.Enqueue(message);
            }
        }

        private void SendThread()
        {
            while (IsActive)
            {
                lock (mSendMessageQueue)
                {
                    while (mSendMessageQueue.Count > 0)
                    {
                        MessageInfo message = mSendMessageQueue.Dequeue();

                        if (message == null) continue;
                        try
                        {
                            message.Session.TcpSocket.Send(message.Buffer.DataBuffer);
                        }
                        catch (SocketException e)
                        {
                            Debug.Log("TCP SocketException 发送消息错误：" + e.Message);
                            message.Session.Disconnect();
                        }
                        catch (Exception e)
                        {
                            Debug.Error("TCP Exception 发送消息错误：" + e.Message);
                            throw e;
                        }
                    }
                }
                Thread.Sleep(1);
            }
        }

        private void AcceptThread()
        {
            while (IsActive)
            {
                try
                {
                    Socket tcp_Socket = AcceptSocket();

                    if (tcp_Socket != null)
                    {
                        Session client = new Session(numberOfClient++, tcp_Socket);
                        Debug.Log("有客户端连接："+ tcp_Socket.RemoteEndPoint);
                        client.OnReceiveMessage_TCP += AnalyzeMessage;
                        lock (mClientSessions)
                        {
                            mClientSessions.Add(client);
                        }
                    }
                    Thread.Sleep(1);
                }
                catch (Exception e)
                {
                    Debug.Error(e.Message);
                    throw e;
                }
            }
        }

        private void ReceiveThread()
        {
            while (IsActive)
            {
                for (int i = 0; i < mClientSessions.Count; ++i)
                {
                    Session clientSession = mClientSessions[i];
                    if (clientSession == null || clientSession.IsConnected == false) continue;

                    try
                    {
                        int receiveSize = clientSession.TcpSocket.Receive(MessageBuffer.head, MessageBuffer.MESSAGE_HEAD_SIZE, SocketFlags.None);
                        if (receiveSize == 0) continue;// 消息大小为0 跳过
                        if (receiveSize != MessageBuffer.MESSAGE_HEAD_SIZE) continue;
                        if (MessageBuffer.IsValid(MessageBuffer.head) == false) continue;
                        int bodySize = 0;
                        if (MessageBuffer.Decode(MessageBuffer.head, MessageBuffer.MESSAGE_BODY_SIZE_OFFSET, ref bodySize) == false) continue;
                        MessageBuffer message = new MessageBuffer(MessageBuffer.MESSAGE_HEAD_SIZE + bodySize);

                        Array.Copy(MessageBuffer.head, 0, message.DataBuffer, 0, MessageBuffer.head.Length);
                        if (bodySize > 0)
                        {
                            int receiveBodySize = clientSession.TcpSocket.Receive(message.DataBuffer, MessageBuffer.MESSAGE_BODY_OFFSET, bodySize, SocketFlags.None);

                            if (receiveBodySize != bodySize) continue;
                        }
                        clientSession.OnReceiveMessageTCP(message);
                    }
                    catch (SocketException e)
                    {
                        Debug.Log("TCP SocketException 接收消息：" + e.Message);
                        clientSession.Disconnect();
                    }
                    catch (Exception e)
                    {
                        Debug.Error("TCP Exception 接收消息：" + e.Message);
                        throw e;
                    }
                }
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 这个位置只处理 登录消息、 注册消息
        /// </summary>
        /// <param name="messageInfo"></param>
        public void AnalyzeMessage(MessageInfo messageInfo)
        {
            MessageID id = (MessageID)messageInfo.Buffer.Id();
            switch (id)
            {
                case MessageID.CsReguestRegister:
                    MessageUserRegister(messageInfo);
                    break;
                case MessageID.CsReguestLogin:
                    MessageUserLogin(messageInfo);
                    break;
                case MessageID.CsReguestMatch:
                    MessageRequestMatch(messageInfo);
                    break;
                case MessageID.CsReguestCancalMatch:
                    MessageCancelMatch(messageInfo);
                    break;
                case MessageID.CsReguestPing:
                    MessagePing(messageInfo);
                    break;
            }
        }

        // 注册账号
        private void MessageUserRegister(MessageInfo messageInfo)
        {
            Debug.Log("注册账号");
            RequestRegigter _info = ProtoTransfer.DeserializeProtoBuf3<RequestRegigter>(messageInfo.Buffer);
            bool isUsing = UserManager.Instance.IsValidAccount(_info.Account);
            ResponseRegister _result = new ResponseRegister();
            if (isUsing)
            {
                _result.Result = false;
                _result.Reason = "账号已存在！";
            }
            else
            {
                _result.Result = true;
                UserManager.Instance.AddUser(_info.Account, messageInfo.Session, new UserAccountData(_info.Account, _info.Password));
            }
            MessageBuffer _message = new MessageBuffer((int)MessageID.ScResponseRegister, ProtoTransfer.SerializeProtoBuf3(_result), 0);

            Send(new MessageInfo(_message, messageInfo.Session));

            Debug.Log("注册账号：" + _result.Result.ToString());
        }
        // 用户登录
        private void MessageUserLogin(MessageInfo messageInfo)
        {
            Debug.Log("账号登录");
            RequestLogin _info = ProtoTransfer.DeserializeProtoBuf3<RequestLogin>(messageInfo.Buffer);
            User user = UserManager.Instance.GetUserByAccount(_info.Account);
            ResponseLogin _result = new ResponseLogin();
            if (user != null)
            {
                if (user.AccountData.Password == _info.Password)
                {
                    if (user.UserState == UserState.OffLine)
                    {
                        UserManager.Instance.UserLogin(_info.Account);
                        _result.Result = true;
                        _result.Token = user.Token;
                    }
                    else
                    {
                        _result.Result = false;
                        _result.Reason = "账号已经登录";
                    }
                }
                else
                {
                    _result.Result = false;
                    _result.Reason = "账号或者密码错误";
                }
            }
            else
            {
                _result.Result = false;
                _result.Reason = "账号不存在";
            }

            MessageBuffer _message = new MessageBuffer((int)MessageID.ScResponseLogin, ProtoTransfer.SerializeProtoBuf3(_result), user == null ? 0 : user.Id);
            Debug.Log("账号登录：" + _result.Result.ToString());
            Send(new MessageInfo(_message, messageInfo.Session));
        }
        // 请求匹配
        private void MessageRequestMatch(MessageInfo messageInfo)
        {
            RequestMatch _info = ProtoTransfer.DeserializeProtoBuf3<RequestMatch>(messageInfo.Buffer);
            User user = UserManager.Instance.GetUserByToken(_info.Token);
            MatchManager.Instance.AddMatchUser(user);
            MessageBuffer _message = new MessageBuffer((int)MessageID.ScResponseMatch, ProtoTransfer.SerializeProtoBuf3(new ResponseRequestMatch()), user.Id);
            Debug.Log("用户请求匹配");
            Send(new MessageInfo(_message, messageInfo.Session));
        }
        // 取消匹配
        private void MessageCancelMatch(MessageInfo messageInfo)
        {
            RequestCancelMatch _info = ProtoTransfer.DeserializeProtoBuf3<RequestCancelMatch>(messageInfo.Buffer);
            User user = UserManager.Instance.GetUserByToken(_info.Token);
            bool _result = MatchManager.Instance.CancleMatch(user);
            if (_result && user!=null)
            {
                MessageBuffer _message = new MessageBuffer((int)MessageID.ScResponseCancelMatch, ProtoTransfer.SerializeProtoBuf3(new ResponseCancelMatch()), user.Id);
                Debug.Log("用户取消匹配");
                Send(new MessageInfo(_message, messageInfo.Session));
            }
        }
        // Ping
        private void MessagePing(MessageInfo messageInfo)
        {
            RequestPing requestPing = ProtoTransfer.DeserializeProtoBuf3<RequestPing>(messageInfo.Buffer);
            User user = UserManager.Instance.GetUserByToken(requestPing.Token);
            if (user != null)
            {
                ResponsePing responsePing = new ResponsePing()
                {
                    Uid = user.Id,
                    Token = user.Token
                };
                MessageBuffer _message = new MessageBuffer((int)MessageID.ScResponsePing, ProtoTransfer.SerializeProtoBuf3(responsePing), user.Id);
                Send(new MessageInfo(_message, messageInfo.Session));
            }
        }

        public Session GetSession(int id)
        {
            Session s = null;
            lock (mClientSessions)
            {
                for (int i = 0; i < mClientSessions.Count; i++)
                {
                    if (mClientSessions[i].Id == id)
                    {
                        s = mClientSessions[i]; break;
                    }
                }
            }
            return s;
        }

        public Session GetSession(IPEndPoint ip)
        {
            Session s = null;
            lock (mClientSessions)
            {
                for (int i = 0; i < mClientSessions.Count; i++)
                {
                    if (mClientSessions[i].udpAdress != null)
                    {
                        if (mClientSessions[i].udpAdress.AddressFamily == ip.AddressFamily
                            && mClientSessions[i].udpAdress.Address.Equals(ip.Address)
                            && mClientSessions[i].udpAdress.Port == ip.Port)
                        {
                            s = mClientSessions[i]; break;
                        }
                    }
                }
            }
            return s;
        }
    }
}
