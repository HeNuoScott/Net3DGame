using PBMessage;
using System;

namespace Server
{
    public class Command
    {
        private long mID;
        private long mFrame;
        private int mType;
        private byte[] mData;
        private long mTime = 0;

        public long Id { get { return mID; } }
        /// <summary>
        /// 帧索引
        /// </summary>
        public long Frame { get { return mFrame; } }
        /// <summary>
        /// 消息类型 ServerToClientID
        /// </summary>
        public int Type { get { return mType; } }
        /// <summary>
        /// 数据
        /// </summary>
        public byte[] Data { get { return mData; } }
        /// <summary>
        /// 时间
        /// </summary>
        public long Time { get { return mTime; } }

        public Command() { }

        public Command(long frame, int type, byte[] data, long time)
        {
            mID = GUID.Int64();
            mFrame = frame;
            mType = type;
            mData = data;
            mTime = time;
        }

        public void Set<T>(CommandID type, T t) where T : Google.Protobuf.IMessage
        {
            mType = (int)type;
            mData = ProtoTransfer.SerializeProtoBuf3<T>(t);
        }

        public void SetFrame(long frame, long time)
        {
            mFrame = frame;
            mTime = time;
        }

        public T Get<T>() where T : Google.Protobuf.IMessage, new()
        {
            T t = ProtoTransfer.DeserializeProtoBuf3<T>(mData);
            return t;
        }

        public object Get(Type type)
        {
            return ProtoTransfer.DeserializeProtoBuf(mData, type);
        }
    }
}
