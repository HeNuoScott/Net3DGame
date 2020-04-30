using System.Collections.Generic;
using System.Threading;
using System.Linq;
using PBMessage;
using System;

namespace Server
{
    public class BattleController
    {
        private Mode mMode;          // 模式
        private Protocol mProtocol;  // 协议
        private int mFrameInterval;  // 帧间隔 ms
        private int mBattleId = 0;   // 战斗Id
        private int mRandSeed = 0;  // 随机数种子
        private int mRoleId = 100;   //客户端的人物开始id
        private int mMonsterId = 100000; //客户端怪物开始id
        private const int SERVER_ROLEID = 0; //服务器也参与整局游戏，负责发送一些全局命令，比如Buff、怪物生成

        Thread mUpdateThread;
        List<Player> mPlayerList = new List<Player>(); // 所有玩家
        List<Monster> mMonsterList = new List<Monster>(); // 游戏中的怪物
        Dictionary<long, Dictionary<int, List<Command>>> mFrameDic = new Dictionary<long, Dictionary<int, List<Command>>>();//关键帧

        private bool mBegin = false;    //游戏是否开始
        private bool mGameOver = false; //游戏是否结束
        private long mCurrentFrame = 1; //当前帧数
        private long mFrameTime = 0;


        /// <summary>
        /// 构建一个战斗控制器
        /// </summary>
        public void CreatBattle(int _battleID, List<User> _battleUser)
        {
            mMode = ServerConfig.MODE;
            mProtocol = ServerConfig.PROTO;
            mFrameInterval = ServerConfig.FRAME_INTERVAL;
            mBattleId = _battleID;


            mUpdateThread = new Thread(UpdateThread);
            mUpdateThread.Start();
        }

        /// <summary>
        /// 广播消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msaageId"></param>
        /// <param name="data"></param>
        /// <param name="ready">是否只发给已经准备好的人</param>
        public void BroadCast<T>(MessageID msaageId, T data, bool ready = false) where T : Google.Protobuf.IMessage
        {
            for (int i = 0; i < mPlayerList.Count; ++i)
            {
                if (ready == false || (ready == true && mPlayerList[i].Ready))
                {
                    if (mProtocol == Protocol.UDP)
                    {
                        mPlayerList[i].SendUdp(msaageId, data);
                    }
                    else if (mProtocol == Protocol.KCP)
                    {
                        mPlayerList[i].SendKcp(msaageId, data);
                    }
                    
                }
            }
        }

        /// <summary>
        /// 获取玩家
        /// </summary>
        public Player GetPlayer(int roleid)
        {
            for (int i = 0; i < mPlayerList.Count; ++i)
            {
                if (mPlayerList[i].Roleid == roleid)
                {
                    return mPlayerList[i];
                }
            }
            return null;
        }

        public void UpdateThread()
        {
            while (mGameOver == false)
            {
                if (mBegin)
                {
                    Thread.Sleep(1);

                    mFrameTime += 1;

                    if (mMode == Mode.Optimistic)
                    {
                        if (mFrameTime % mFrameInterval == 0)
                        {
                            SendFrame();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 按固定频率向客户端广播帧
        /// </summary>
        private void SendFrame()
        {
            long frame = mCurrentFrame++;
            int playerCount = 0; //当前帧有多少个客户端发了命令
            BroadPlayerFrameCommand sendData = new BroadPlayerFrameCommand();

            sendData.Frame = frame;
            sendData.Frametime = mFrameTime;

            if (mFrameDic.ContainsKey(frame))
            {
                Dictionary<int, List<Command>> frames = mFrameDic[frame];

                playerCount = frames.Count;

                var it = frames.GetEnumerator(); // 循环访问构造器
                while (it.MoveNext())
                {
                    for (int i = 0, count = it.Current.Value.Count; i < count; ++i)
                    {
                        OperationCommand cmd = ProtoTransfer.Get(it.Current.Value[i]);
                        sendData.Commands.Add(cmd);
                    }
                }
            }


            //不显示那么多log
            if (frame % 30 == 0 || sendData.Commands.Count > 0)
            {
                Debug.Log(string.Format("Send frame:{0} user count:{1} command count:{2}", frame, playerCount, sendData.Commands.Count), ConsoleColor.Gray);
            }

            BroadCast(MessageID.BroadCmdFrame, sendData, true);
        }

        // 匹配完成之后的消息解析
        public void AnalyzeMessage(MessageInfo messageInfo)
        {
            //ClientToServerID id = (ClientToServerID)messageInfo.Buffer.Id();
            //switch (id)
            //{
            //    case ClientToServerID.UdpBattleReady:
            //        GM_Ready recvData = ProtoTransfer.DeserializeProtoBuf3<GM_Ready>(messageInfo.Buffer);
            //        OnReceiveReady(messageInfo.Session, recvData);
            //        break;
            //    case ClientToServerID.UdpUpPlayerOperations:
            //        GM_Frame recvData2 = ProtoTransfer.DeserializeProtoBuf3<GM_Frame>(messageInfo.Buffer);
            //        if (mMode == Mode.LockStep)
            //        {
            //            OnLockStepFrame(messageInfo.Session, recvData2);
            //        }
            //        else
            //        {
            //            OnOptimisticFrame(messageInfo.Session, recvData2);
            //        }
            //        break;
            //    case ClientToServerID.UdpUpDeltaFrames:
            //        break;
            //    case ClientToServerID.UdpUpGameOver:
            //        break;
                //case MessageID.GM_PING_CS:
                //    {
                //        GM_Request recvData = ProtoTransfer.DeserializeProtoBuf<GM_Request>(msg);
                //        User u = GetUser(recvData.id);
                //        if (u != null)
                //        {
                //            GM_Return sendData = new GM_Return();
                //            sendData.id = recvData.id;
                //            u.SendUdp(MessageID.GM_PING_SC, sendData);
                //        }
                //    }
                //    break;
            //    default:
            //        break;
            //}
        }

        private void OnReceiveReady(Session client, BroadPlayerReady recvData)
        {
            if (recvData == null || client == null) return;
            int readyCount = 0;
            for (int i = 0; i < mPlayerList.Count; ++i)
            {
                Player player = mPlayerList[i];
                if (recvData.RoleId == player.Roleid && client == player.Client)
                {
                    player.Position = ProtoTransfer.Get(recvData.Position);
                    player.Direction = ProtoTransfer.Get(recvData.Direction);
                    player.SetReady();
                }
                //广播玩家准备（包括自己）
                if (mProtocol == Protocol.UDP)
                {
                    player.SendUdp(MessageID.BroadRoomOperation, recvData);
                }
                else if (mProtocol == Protocol.KCP)
                {
                    player.SendKcp(MessageID.BroadRoomOperation, recvData);
                }

                if (player.Ready) readyCount++;
            }

            if (mBegin == false)
            {
                //所有的玩家都准备好了，可以开始同步
                if (readyCount >= mPlayerList.Count)
                {
                    mFrameDic = new Dictionary<long, Dictionary<int, List<Command>>>();

                    BroadBattleGameStart sendData = new BroadBattleGameStart();
                    sendData.BattleID = mBattleId;
                    sendData.RandSeed = mRandSeed;
                    sendData.UdpPort = ServerConfig.UDP_PORT;
                    sendData.FrameInterval = mFrameInterval;

                    BroadCast(MessageID.BroadBattleStart, sendData, true);

                    BeginGame();

                }
            }

            else //断线重连
            {
                Player user = GetPlayer(recvData.RoleId);
                if (user != null)
                {
                    BroadBattleGameStart sendData = new BroadBattleGameStart();
                    sendData.BattleID = mBattleId;
                    sendData.RandSeed = mRandSeed;
                    sendData.UdpPort = ServerConfig.UDP_PORT;
                    sendData.FrameInterval = mFrameInterval;

                    //user.SendUdp(MessageID.GM_BEGIN_BC, sendData);

                    /*
                    GM_Frame_BC frameData = new GM_Frame_BC();
                    
                    //给他发送当前帧之前的数据
                    for (long frame = 1; frame < mCurrentFrame - 1; ++frame)
                    {
                        if (mFrameDic.ContainsKey(frame))
                        {
                            frameData.frame = frame;
                            frameData.frametime = 0;
                            var it = mFrameDic[frame].GetEnumerator();
                            while (it.MoveNext())
                            {
                                for (int i = 0, count = it.Current.Value.Count; i < count; ++i)
                                {
                                    GMCommand cmd = ProtoTransfer.Get(it.Current.Value[i]);

                                    frameData.command.Add(cmd);
                                }
                            }
                            user.SendUdp(MessageID.GM_FRAME_BC, frameData);
                        }
                    }
                    */
                }
            }

        }

        private void BeginGame()
        {
            mCurrentFrame = 1;

            mBegin = true; //游戏开始

            mFrameTime = 0;

            //服务器添加命令
            for (int i = 0; i < 3; ++i)
            {
                Monster monster = new Monster(mMonsterId++);
                mMonsterList.Add(monster);

                monster.mLifeEntity.name = "Server " + monster.Id;
                monster.mLifeEntity.type = 2;//Boss

                monster.Position = new UnityEngine.Vector3Int(((i + 1) * (i % 2 == 0 ? -3 : 3)) * 10000, 1 * 10000, -10 * 10000);

                CMD_CreateMonster data = new CMD_CreateMonster();
                data.Id = SERVER_ROLEID;//服务器也参与整局游戏，负责发送一些全局命令，比如Buff、怪物生成
                data.Monster = ProtoTransfer.Get(monster.mLifeEntity);
                data.Position = ProtoTransfer.Get(monster.Position);
                data.Direction = ProtoTransfer.Get(monster.Direction);

                Command cmd = new Command();
                cmd.Set(CommandID.CreateMonster, data);

                AddCommand(cmd);
            }
        }

        /// <summary>
        /// 服务器添加一个命令
        /// </summary>
        private void AddCommand(Command cmd)
        {
            if (cmd == null) return;

            if (mFrameDic.ContainsKey(mCurrentFrame) == false)
            {
                mFrameDic[mCurrentFrame] = new Dictionary<int, List<Command>>();
            }

            cmd.SetFrame(mCurrentFrame, mFrameTime);

            if (mFrameDic[mCurrentFrame].ContainsKey(SERVER_ROLEID) == false)
            {
                mFrameDic[mCurrentFrame].Add(SERVER_ROLEID, new List<Command>());
            }
            mFrameDic[mCurrentFrame][SERVER_ROLEID].Add(cmd);
        }

        // 帧同步  ----->收到所有玩家的操作帧之后  进行帧的驱动
        private void OnLockStepFrame(Session client, PlayerFrameCommand recvData)
        {
            long frame = recvData.Frame;
            int roleId = recvData.RoleId;

            if (recvData.Commands.Count > 0 || frame % 30 == 0)
            {
                Debug.Log(string.Format("Receive {0} serverframe:{1} clientframe:{2} command:{3}", roleId, mCurrentFrame, frame, recvData.Commands.Count), ConsoleColor.DarkGray);
            }
            if (mFrameDic.ContainsKey(frame) == false)
            {
                // 添加到当前帧的玩家操作指令中
                mFrameDic.Add(frame, new Dictionary<int, List<Command>>());
            }

            var frames = mFrameDic[frame];

            //当前帧的服务器命令
            if (frames.ContainsKey(SERVER_ROLEID) == false)
            {
                frames.Add(SERVER_ROLEID, new List<Command>());
            }

            //该玩家是否发送了当前帧
            if (frames.ContainsKey(roleId) == false)
            {
                frames.Add(roleId, new List<Command>());
            }

            for (int i = 0; i < recvData.Commands.Count; ++i)
            {
                Command cmd = new Command(recvData.Commands[i].Frame, recvData.Commands[i].Type, recvData.Commands[i].Data.ToByteArray(), recvData.Commands[i].Frametime);

                frames[roleId].Add(cmd);
            }

            //当所有玩家都发送了该帧，就可以广播了
            //减去1是因为服务器命令也在当前帧中
            if (frames.Count - 1 >= mPlayerList.Count)
            {
                BroadPlayerFrameCommand sendData = new BroadPlayerFrameCommand();
                sendData.Frame = frame;
                sendData.Frametime = mFrameTime;
                var it = frames.GetEnumerator();
                while (it.MoveNext())
                {
                    for (int i = 0, count = it.Current.Value.Count; i < count; ++i)
                    {
                        OperationCommand cmd = ProtoTransfer.Get(it.Current.Value[i]);
                        sendData.Commands.Add(cmd);
                    }
                }


                BroadCast(MessageID.BroadCmdFrame, sendData, true);

                mCurrentFrame = frame + 1;
            }
            else
            {
                Debug.Log(string.Format("Waiting {0} frame:{1} count:{2} current:{3} ", roleId, frame, mFrameDic[frame].Count, mPlayerList.Count), ConsoleColor.Red);
            }
        }
        // 乐观帧同步  ---->以服务器时间为准   只要时间到  无论这帧是否包含所有玩家操作  都进行 广播进行帧驱动
        private void OnOptimisticFrame(Session client, PlayerFrameCommand recvData)
        {

            int roleId = recvData.RoleId;

            long frame = recvData.Frame;

            Debug.Log(string.Format("Receive roleid={0} serverframe:{1} clientframe:{2} command:{3}", roleId, mCurrentFrame, frame, recvData.Commands.Count), ConsoleColor.DarkYellow);

            if (mFrameDic.ContainsKey(mCurrentFrame) == false)
            {
                mFrameDic[mCurrentFrame] = new Dictionary<int, List<Command>>();
            }
            for (int i = 0; i < recvData.Commands.Count; ++i)
            {
                //乐观模式以服务器收到的时间为准
                Command frameData = new Command(recvData.Commands[i].Frame, recvData.Commands[i].Type, recvData.Commands[i].Data.ToByteArray(), mFrameTime);
                if (mFrameDic[mCurrentFrame].ContainsKey(roleId) == false)
                {
                    mFrameDic[mCurrentFrame].Add(roleId, new List<Command>());
                }
                mFrameDic[mCurrentFrame][roleId].Add(frameData);
            }
        }
    }
}
