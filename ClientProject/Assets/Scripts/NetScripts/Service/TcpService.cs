using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Network
{
    public class TcpService : TcpClient
    {
        private int mPort;
        private IPAddress mIP;
        private int mConnectTimes = 0;
        private Queue<MessageBuffer> mSendMessageQueue = new Queue<MessageBuffer>();
        private Thread mReceiveThread, mSendThread, mActiveThread;

        public event OnConnectHandler OnConnect;
        public event OnDisconnectHandler OnDisconnect;
        public event OnMessageHandler OnMessage;

        /// <summary>
        /// 是否连接
        /// </summary>
        public bool IsConnected { get { return Client != null && Connected; } }

        /// <summary>
        /// 连接服务器
        /// </summary>
        public new bool Connect(string ip, int port)
        {
            if (IsConnected) return true;

            mIP = IPAddress.Parse(ip);
            mPort = port;
            BeginConnect(mIP, mPort, ConnectResult, this);

            return true;
        }

        private void ConnectResult(IAsyncResult result)
        {
            try
            {
                mConnectTimes += 1;
                // 连接失败
                if (result.IsCompleted == false)
                {
                    EndConnect(result);
                    if (mConnectTimes < 5) BeginConnect(mIP, mPort, ConnectResult, this);
                    else Close();

                    return;
                }

                EndConnect(result);

                if (IsConnected == false)
                {
                    Close();
                    return;
                }

                mReceiveThread = new Thread(ReceiveThread);
                mSendThread = new Thread(SendThread);
                mActiveThread = new Thread(ActiveThread);


                mReceiveThread.Start();
                mSendThread.Start();
                mActiveThread.Start();

                OnConnect?.Invoke();
            }
            catch (Exception e)
            {
                Close();
                throw e;
            }

        }

        public new void Close()
        {
            if (IsConnected) GetStream().Close();
            base.Close();

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
            OnDisconnect?.Invoke();
            OnDisconnect = null;
        }

        public void Send(MessageBuffer message)
        {
            if (message == null) return;
            lock (mSendMessageQueue)
            {
                mSendMessageQueue.Enqueue(message);
            }
        }

        private void ActiveThread()
        {
            while (IsConnected) Thread.Sleep(1000);

            Close();
        }

        private void SendThread()
        {
            while (IsConnected)
            {
                try
                {
                    lock (mSendMessageQueue)
                    {
                        for (int i = 0; i < mSendMessageQueue.Count; ++i)
                        {
                            MessageBuffer message = mSendMessageQueue.Dequeue();

                            if (message == null) continue;
                            UnityEngine.Debug.Log("消息发送");
                            GetStream().Write(message.DataBuffer, 0, message.DataSize);
                        }

                        mSendMessageQueue.Clear();
                    }
                }
                catch (Exception e)
                {
                    Close();
                    throw e;
                }

                Thread.Sleep(1);
            }
        }

        private void ReceiveThread()
        {
            while (IsConnected)
            {
                try
                {

                    int receiveSize = Client.Receive(MessageBuffer.head, MessageBuffer.MESSAGE_HEAD_SIZE, SocketFlags.None);
                    if (receiveSize == 0) continue;
                    if (receiveSize != MessageBuffer.MESSAGE_HEAD_SIZE) continue;
                    if (MessageBuffer.IsValid(MessageBuffer.head) == false) continue;
                    int bodySize = 0;
                    if (MessageBuffer.Decode(MessageBuffer.head, MessageBuffer.MESSAGE_BODY_SIZE_OFFSET, ref bodySize) == false)
                    {
                        continue;
                    }

                    MessageBuffer message = new MessageBuffer(MessageBuffer.MESSAGE_HEAD_SIZE + bodySize);

                    Array.Copy(MessageBuffer.head, 0, message.DataBuffer, 0, MessageBuffer.head.Length);

                    if (bodySize > 0)
                    {
                        int receiveBodySize = Client.Receive(message.DataBuffer, MessageBuffer.MESSAGE_BODY_OFFSET, bodySize, SocketFlags.None);

                        if (receiveBodySize != bodySize) continue;
                    }

                    OnMessage?.Invoke(message);
                }
                catch (Exception e)
                {
                    Close();
                    throw e;
                }

                Thread.Sleep(1);
            }
        }
    }
}