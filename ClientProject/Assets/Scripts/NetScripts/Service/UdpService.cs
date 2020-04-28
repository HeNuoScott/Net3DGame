using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Network
{
    public class UdpService : UdpClient
    {
        private IPEndPoint mServerAdress;
        private Thread mReceiveThread, mSendThread;
        private Queue<MessageBuffer> mSendMessageQueue = new Queue<MessageBuffer>();
        public bool IsConnected { get { return Client != null && Client.Connected; } }

        public event OnConnectHandler OnConnect;
        public event OnMessageHandler OnMessage;
        public event OnDisconnectHandler OnDisconnet;

        public new bool Connect(string ip, int port)
        {
            if (IsConnected) return true;

            mServerAdress = new IPEndPoint(IPAddress.Parse(ip), port);

            Connect(mServerAdress);

            if (IsConnected == false)
            {
                Close();
                return false;
            }

            mReceiveThread = new Thread(ReceiveThread);
            mSendThread = new Thread(SendThread);
            mReceiveThread.Start();
            mSendThread.Start();

            OnConnect?.Invoke();
            return true;
        }

        public void Send(MessageBuffer message)
        {
            if (message == null) return;

            lock (mSendMessageQueue)
            {
                mSendMessageQueue.Enqueue(message);
            }
        }

        public new void Close()
        {
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
            if (OnDisconnet != null)
            {
                OnDisconnet();
                OnDisconnet = null;
            }
        }

        private void SendThread()
        {
            while (IsConnected)
            {
                try
                {
                    lock (mSendMessageQueue)
                    {
                        while (mSendMessageQueue.Count > 0)
                        {
                            MessageBuffer message = mSendMessageQueue.Dequeue();
                            if (message == null) continue;
                            Send(message.DataBuffer, message.DataSize);
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
                    IPEndPoint ip = mServerAdress;
                    byte[] data = Receive(ref ip);

                    if (data.Length > 0)
                    {
                        if (MessageBuffer.IsValid(data))
                        {
                            OnMessage?.Invoke(new MessageBuffer(data));
                        }
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
    }
}
