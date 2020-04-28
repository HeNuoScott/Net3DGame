using System;
using System.IO;

namespace Server
{
    /// <summary>
    /// Proto协议消息 序列化与反序列化
    /// </summary>
    public static class ProtoTransfer
    {
        /// <summary>
        /// 序列化泛型类
        /// </summary>
        public static byte[] SerializeProtoBuf2<T>(T data) where T : class, ProtoBuf.IExtensible
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize<T>(ms, data);
                byte[] bytes = ms.ToArray();

                ms.Close();

                return bytes;
            }
        }
        public static byte[] SerializeProtoBuf3<T>(T data) where T : Google.Protobuf.IMessage
        {
            using (MemoryStream rawOutput = new MemoryStream())
            {
                Google.Protobuf.CodedOutputStream output = new Google.Protobuf.CodedOutputStream(rawOutput);
                //output.WriteRawVarint32((uint)len);
                output.WriteMessage(data);
                output.Flush();
                byte[] result = rawOutput.ToArray();

                return result;
            }
        }

        /// <summary>
        /// 序列化解析
        /// </summary>
        public static T DeserializeProtoBuf2<T>(MessageBuffer buffer) where T : class, ProtoBuf.IExtensible
        {
            return DeserializeProtoBuf2<T>(buffer.Body());
        }
        public static T DeserializeProtoBuf3<T>(MessageBuffer buffer) where T : Google.Protobuf.IMessage, new()
        {
            return DeserializeProtoBuf3<T>(buffer.Body());
        }

        /// <summary>
        /// 序列化解析
        /// </summary>
        public static T DeserializeProtoBuf2<T>(byte[] bytes) where T : class, ProtoBuf.IExtensible
        {
            if (bytes == null) return default(T);

            using (MemoryStream ms = new MemoryStream(bytes))
            {
                T t = ProtoBuf.Serializer.Deserialize<T>(ms);
                return t;
            }
        }
        public static T DeserializeProtoBuf3<T>(byte[] bytes) where T : Google.Protobuf.IMessage, new()
        {
            if (bytes == null) return default(T);
            Google.Protobuf.CodedInputStream stream = new Google.Protobuf.CodedInputStream(bytes);
            T msg = new T();
            stream.ReadMessage(msg);
            //msg= (T)msg.Descriptor.Parser.ParseFrom(dataBytes);
            return msg;
        }

        public static object DeserializeProtoBuf(byte[] data, Type type)
        {
            if (data == null)
            {
                return null;
            }
            using (MemoryStream ms = new MemoryStream(data))
            {
                return ProtoBuf.Meta.RuntimeTypeModel.Default.Deserialize(ms, null, type);
            }
        }
    }
}
