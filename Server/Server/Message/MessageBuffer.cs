using System;

namespace Server
{
    public class MessageBuffer
    {
        byte[] mBuffer;

        public const int MESSAGE_ID_OFFSET = 0;         //包ID偏移
        public const int MESSAGE_BODY_SIZE_OFFSET = 4;  //包体大小偏移
        public const int MESSAGE_VERSION_OFFSET = 8;    //包版本偏移
        public const int MESSAGE_EXTRA_OFFSET = 12;     //额外数据
        public const int MESSAGE_BODY_OFFSET = 16;      //包体偏移
        public const int MESSAGE_HEAD_SIZE = 16;        //包头大小
        public const int MESSAGE_VERSION = 1;

        public static int MESSAGE_MAX_VALUE = 1000;
        public static int MESSAGE_MIN_VALUE = 0;

        //定义一个静态的包头
        public static byte[] head = new byte[MESSAGE_HEAD_SIZE];

        public MessageBuffer(int length)
        {
            mBuffer = new byte[length];
        }

        public MessageBuffer(byte[] data)
        {
            mBuffer = new byte[data.Length];
            Buffer.BlockCopy(data, 0, mBuffer, 0, data.Length);
        }

        public MessageBuffer(int messageId, byte[] data, int extra)
        {
            mBuffer = new byte[MESSAGE_HEAD_SIZE + data.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(messageId), 0, mBuffer, MESSAGE_ID_OFFSET, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, mBuffer, MESSAGE_BODY_SIZE_OFFSET, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(MESSAGE_VERSION), 0, mBuffer, MESSAGE_VERSION_OFFSET, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(extra), 0, mBuffer, MESSAGE_EXTRA_OFFSET, 4);
            Buffer.BlockCopy(data, 0, mBuffer, MESSAGE_BODY_OFFSET, data.Length);
        }


        /// <summary>
        /// 数据是否有效
        /// </summary>
        public static bool IsValid(byte[] buffer)
        {
            if (buffer == null) return false;

            if (buffer.Length < MESSAGE_HEAD_SIZE) return false;

            int messageId = 0;
            if (Decode(buffer, MESSAGE_ID_OFFSET, ref messageId) == false)
            {
                return false;
            }

            int version = 0;
            if (Decode(buffer, MESSAGE_VERSION_OFFSET, ref version) == false)
            {
                return false;
            }

            if (messageId > MESSAGE_MIN_VALUE && messageId < MESSAGE_MAX_VALUE && version == MESSAGE_VERSION)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 解码数据  并且返回数值
        /// </summary>
        /// <param name="buffer">数据</param>
        /// <param name="offset">例如： 消息id offset 对应返回消息id，版本offset 对应返回版本id </param>
        /// <param name="value">返回数值ref</param>
        /// <returns></returns>
        public static bool Decode(byte[] buffer, int offset, ref int value)
        {
            if (buffer == null || buffer.Length < MESSAGE_HEAD_SIZE || offset + 4 > buffer.Length)
            {
                return false;
            }

            value = BitConverter.ToInt32(buffer, offset);

            return true;
        }

        /// <summary>
        /// 二进制数据
        /// </summary>
        public byte[] DataBuffer { get { return mBuffer; } }

        /// <summary>
        /// 数据的大小
        /// </summary>
        public int DataSize { get { return mBuffer.Length; } }

        /// <summary>
        /// 消息是否有效
        /// </summary>
        public bool IsValid()
        {
            return IsValid(mBuffer);
        }

        /// <summary>
        /// 消息Id
        /// </summary>
        public int Id()
        {
            int messageid = -1;

            Decode(mBuffer, MESSAGE_ID_OFFSET, ref messageid);

            return messageid;
        }

        /// <summary>
        /// 消息版本
        /// </summary>
        public int Version()
        {
            int version = -1;
            Decode(mBuffer, MESSAGE_VERSION_OFFSET, ref version);
            return version;
        }

        /// <summary>
        /// 消息额外数据
        /// </summary>
        public int Extra()
        {
            int extra = -1;
            Decode(mBuffer, MESSAGE_EXTRA_OFFSET, ref extra);
            return extra;
        }

        /// <summary>
        /// 消息包体
        /// </summary>
        public byte[] Body()
        {
            int bodySize = -1;
            if (Decode(mBuffer, MESSAGE_BODY_SIZE_OFFSET, ref bodySize))
            {
                byte[] body = new byte[bodySize];
                Buffer.BlockCopy(mBuffer, MESSAGE_BODY_OFFSET, body, 0, bodySize);
                return body;
            }
            return null;
        }
    }
}