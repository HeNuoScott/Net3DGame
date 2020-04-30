using UnityEngine;
using PBMessage;

namespace Server
{
    /// <summary>
    /// 战场中的玩家
    /// </summary>
    public class Player
    {
        private int mRoleId;
        private bool mReady;
        private Session mClient;
        private Vector3Int mPosition;
        private Vector3Int mDirection;

        public int Roleid { get { return mRoleId; } }
        public Session Client { get { return mClient; } }
        public bool Ready { get { return mReady; } }
        public Vector3Int Position { get { return mPosition; } set { mPosition = value; } }
        public Vector3Int Direction { get { return mDirection; } set { mDirection = value; } }

        public LifeEntity mPlayerInfo = new LifeEntity();

        public Player(int roleId, Session client)
        {
            mRoleId = roleId;
            mClient = client;
            mReady = false;

            mPlayerInfo.roleid = Roleid;
            mPlayerInfo.name = mRoleId.ToString();
            mPlayerInfo.moveSpeed = 500;
            mPlayerInfo.moveSpeedAddition = 0;
            mPlayerInfo.moveSpeedPercent = 0;
            mPlayerInfo.attackSpeed = 100;
            mPlayerInfo.attackSpeedAddition = 0;
            mPlayerInfo.attackSpeedPercent = 0;
            mPlayerInfo.maxBlood = 100;
            mPlayerInfo.nowBlood = 100;
            mPlayerInfo.type = 0; //人物
        }

        /// <summary>
        /// 设置准备
        /// </summary>
        public void SetReady()
        {
            mReady = true;
        }

        public void SendUdp<T>(MessageID messageId, T t) where T : Google.Protobuf.IMessage
        {
            if (mClient != null)
            {
                byte[] data = ProtoTransfer.SerializeProtoBuf3<T>(t);
                MessageBuffer message = new MessageBuffer((int)messageId, data, mClient.Id);
                mClient.SendUdp(message);
            }
        }

        public void SendTcp<T>(MessageID messageId, T t) where T : Google.Protobuf.IMessage
        {
            if (mClient != null)
            {
                byte[] data = ProtoTransfer.SerializeProtoBuf3<T>(t);
                MessageBuffer message = new MessageBuffer((int)messageId, data, mClient.Id);
                mClient.SendTcp(message);
            }
        }

        public void SendKcp<T>(MessageID messageId, T t) where T : Google.Protobuf.IMessage
        {
            if (mClient != null)
            {
                byte[] data = ProtoTransfer.SerializeProtoBuf3<T>(t);
                MessageBuffer message = new MessageBuffer((int)messageId, data, mClient.Id);
                mClient.SendKcp(message);
            }
        }

        public void Disconnect()
        {
            mClient.Disconnect();
        }
    }
}
