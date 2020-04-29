using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System;
using PBCommon;
using PBLogin;
using PBMatch;

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
                        Session c = new Session(numberOfClient++, tcp_Socket);
                        Debug.Log("有客户端连接："+ tcp_Socket.RemoteEndPoint);
                        c.OnReceiveMessage_TCP += AnalyzeMessage;
                        lock (mClientSessions)
                        {
                            mClientSessions.Add(c);
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
                        clientSession.OnReceiveMessageTCP(new MessageInfo(message, clientSession));
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
            ClientToServerID id = (ClientToServerID)messageInfo.Buffer.Id();
            switch (id)
            {
                case ClientToServerID.TcpRegister://注册
                    UserRegister(messageInfo);
                    break;
                case ClientToServerID.TcpLogin://登录
                    UserLogin(messageInfo);
                    break;
                case ClientToServerID.TcpRequestMatch://匹配
                    RequestMatch(messageInfo);
                    break;
                case ClientToServerID.TcpCancelMatch://取消匹配
                    CancelMatch(messageInfo);
                    break;
                default:
                    break;
            }
        }


        // 取消匹配
        private void CancelMatch(MessageInfo messageInfo)
        {
            TcpCancelMatch _info = ProtoTransfer.DeserializeProtoBuf3<TcpCancelMatch>(messageInfo.Buffer);
            User user = UserManager.Instance.GetUserByToken(_info.Token);
            bool _result = MatchManager.Instance.CancleMatch(user);
            if (_result)
            {
                byte[] bytes = ProtoTransfer.SerializeProtoBuf3<TcpResponseCancelMatch>(new TcpResponseCancelMatch());
                MessageBuffer _message = new MessageBuffer((int)ServerToClientID.TcpResponseCancelMatch, bytes, 0);
                MessageInfo _messageInfo = new MessageInfo(_message, messageInfo.Session);
                Debug.Log("用户取消匹配");
                Send(_messageInfo);
            }
        }

        // 请求匹配
        private void RequestMatch(MessageInfo messageInfo)
        {
            TcpRequestMatch _info = ProtoTransfer.DeserializeProtoBuf3<TcpRequestMatch>(messageInfo.Buffer);
            User user = UserManager.Instance.GetUserByToken(_info.Token);
            MatchManager.Instance.AddMatchUser(user);
            byte[] bytes = ProtoTransfer.SerializeProtoBuf3<TcpResponseRequestMatch>(new TcpResponseRequestMatch());
            MessageBuffer _message = new MessageBuffer((int)ServerToClientID.TcpResponseRequestMatch, bytes, 0);
            MessageInfo _messageInfo = new MessageInfo(_message, messageInfo.Session);
            Debug.Log("用户请求匹配");
            Send(_messageInfo);
        }

        // 注册账号
        private void UserRegister(MessageInfo messageInfo)
        {
            Debug.Log("注册账号");
            TcpRegister _info = ProtoTransfer.DeserializeProtoBuf3<TcpRegister>(messageInfo.Buffer);
            //string token = TokenHelper.GenToken(_info.Account);
            string token = _info.Account;
            bool isUsing = UserManager.Instance.TokenIsValid(token);
            TcpResponseRegister _result = new TcpResponseRegister();
            if (isUsing)
            {
                _result.Result = false;
            }
            else
            {
                _result.Result = true;
                _result.Token = token;
                UserManager.Instance.AddUser(token, messageInfo.Session,new UserAccountData(_info.Account,_info.Password));
            }
            byte[] bytes = ProtoTransfer.SerializeProtoBuf3<TcpResponseRegister>(_result);
            MessageBuffer _message = new MessageBuffer((int)ServerToClientID.TcpResponseRegister, bytes, 0);
            MessageInfo _messageInfo = new MessageInfo(_message, messageInfo.Session);
            Debug.Log("注册账号：" + _result.Result.ToString());
            Send(_messageInfo);
        }

        // 用户登录
        private void UserLogin(MessageInfo messageInfo)
        {
            Debug.Log("账号登录");
            TcpLogin _info = ProtoTransfer.DeserializeProtoBuf3<TcpLogin>(messageInfo.Buffer);
            //string token = TokenHelper.GenToken(_info.Account);
            string token = _info.Account;
            bool IsValid = UserManager.Instance.TokenIsValid(token);
            TcpResponseLogin _result = new TcpResponseLogin();
            if (IsValid)
            {
                User user = UserManager.Instance.GetUserByToken(token);
                if (user.AccountData.Password==_info.Password)
                {
                    if (user.UserState == UserState.OffLine)
                    {
                        user.UserState = UserState.OnLine;
                        _result.Result = true;
                        _result.Uid = user.Id;
                        _result.Token = token;
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
            byte[] bytes = ProtoTransfer.SerializeProtoBuf3<TcpResponseLogin>(_result);
            MessageBuffer _message = new MessageBuffer((int)ServerToClientID.TcpResponseLogin, bytes, 0);
            MessageInfo _messageInfo = new MessageInfo(_message, messageInfo.Session);
            Debug.Log("账号登录：" + _result.Result.ToString());
            Send(_messageInfo);
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
