using PBMessage;
using System;
using System.IO;
using UnityEngine;

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


        public static MonsterInfo Get(LifeEntity info)
        {
            MonsterInfo monsterInfo = new MonsterInfo
            {
                Id = info.roleid,
                Name = info.name,
                MoveSpeed = info.moveSpeed,
                MoveSpeedAddition = info.moveSpeedAddition,
                MoveSpeedPercent = info.moveSpeedPercent,
                AttackSpeed = info.attackSpeed,
                AttackSpeedAddition = info.attackSpeedAddition,
                AttackSpeedPercent = info.attackSpeedPercent,
                MaxBlood = info.maxBlood,
                NowBlood = info.nowBlood,
                Type = info.type
            };
            return monsterInfo;
        }

        public static LifeEntity Get(PlayerInfo info)
        {
            LifeEntity lifeEntity = new LifeEntity
            {
                roleid = info.PlayerId,
                name = info.Name,
                moveSpeed = info.MoveSpeed,
                moveSpeedAddition = info.MoveSpeedAddition,
                moveSpeedPercent = info.MoveSpeedPercent,
                attackSpeed = info.AttackSpeed,
                attackSpeedAddition = info.AttackSpeedAddition,
                attackSpeedPercent = info.AttackSpeedPercent,
                maxBlood = info.MaxBlood,
                nowBlood = info.NowBlood,
                type = info.Type
            };
            return lifeEntity;
        }

        public static LifeEntity Get(MonsterInfo info)
        {
            LifeEntity lifeEntity = new LifeEntity
            {
                roleid = info.Id,
                name = info.Name,
                moveSpeed = info.MoveSpeed,
                moveSpeedAddition = info.MoveSpeedAddition,
                moveSpeedPercent = info.MoveSpeedPercent,
                attackSpeed = info.AttackSpeed,
                attackSpeedAddition = info.AttackSpeedAddition,
                attackSpeedPercent = info.AttackSpeedPercent,
                maxBlood = info.MaxBlood,
                nowBlood = info.NowBlood,
                type = info.Type
            };
            return lifeEntity;
        }

        public static OperationCommand Get(Command command)
        {
            OperationCommand cmd = new OperationCommand()
            {
                Id = command.Id,
                Frame = command.Frame,
                Frametime = command.Time,
                Type = command.Type,
                Data = Google.Protobuf.ByteString.CopyFrom(command.Data),
            };
            return cmd;
        }

        public static Vector3Int Get(Point3D gmpoint)
        {
            Vector3Int point = new Vector3Int(gmpoint.X, gmpoint.Y, gmpoint.Z);
            return point;
        }

        public static Point3D Get(Vector3Int point)
        {
            Point3D gmpoint = new Point3D
            {
                X = point.x,
                Y = point.y,
                Z = point.z
            };
            return gmpoint;
        }

    }
}
