using Net.Share;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Net.Server
{
    /// <summary>
    /// 服务器网络行为
    /// </summary>
    public abstract class NetBehaviour
    {
        /// <summary>
        /// 构造网络行为函数
        /// </summary>
        public NetBehaviour() { }

        /// <summary>
        /// 添加带有RPCFun特性的所有方法
        /// </summary>
        public static void AddRPCFuns()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName;
            var exportedTypes = Assembly.LoadFile(path).GetExportedTypes();
            foreach (var type in exportedTypes)
            {
                if (type.IsSubclassOf(typeof(NetBehaviour)) & !type.IsAbstract)
                {
                    try
                    {
                        var target = Activator.CreateInstance(type);
                        foreach (MethodInfo method in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            var rpc = method.GetCustomAttribute<RPCFun>();
                            if (rpc != null)
                            {
                                NetServerBase.Instance.Rpcs.Add(new NetDelegate(target, method, rpc.cmd));
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// 获取所有服务器的类中带有RPCFun特性的方法
        /// </summary>
        public static NetDelegate[] GetAllTypesRPCFuns()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName;
            var exportedTypes = Assembly.LoadFile(path).GetExportedTypes();
            List<NetDelegate> RPCFuns = new List<NetDelegate>();
            foreach (var type in exportedTypes)
            {
                if (type.IsSubclassOf(typeof(NetBehaviour)) & !type.IsAbstract)
                {
                    try
                    {
                        var target = Activator.CreateInstance(type);
                        foreach (MethodInfo method in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            var rpc = method.GetCustomAttribute<RPCFun>();
                            if (rpc != null)
                            {
                                RPCFuns.Add(new NetDelegate(target, method, rpc.cmd));
                            }
                        }
                    }
                    catch { }
                }
            }
            return RPCFuns.ToArray();
        }

        /// <summary>
        /// 添加所有带有RPCFun特性的方法
        /// </summary>
        /// <param name="target">要收集RPCFun特性的对象</param>
        public static void AddRPCFuns(object target)
        {
            foreach (MethodInfo info in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var rpc = info.GetCustomAttribute<RPCFun>();
                if (rpc != null)
                {
                    NetServerBase.Instance.Rpcs.Add(new NetDelegate(target, info, rpc.cmd));
                }
            }
        }

        /// <summary>
        /// 添加所有带有RPCFun特性的方法
        /// </summary>
        /// <param name="server"></param>
        /// <param name="target"></param>
        /// <param name="append">追加rpc，如果target类型已经存在还可以追加到rpcs？</param>
        public static void AddRPCFuns(NetServerBase server, object target, bool append = false)
        {
            if (!append)
                foreach (var o in server.Rpcs)
                    if (o.target == target)
                        return;

            foreach (MethodInfo info in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var rpc = info.GetCustomAttribute<RPCFun>();
                if (rpc != null)
                {
                    server.Rpcs.Add(new NetDelegate(target, info, rpc.cmd));
                }
            }
        }

        /// <summary>
        /// 获取带有RPCFun特性的所有方法
        /// </summary>
        /// <param name="target">要获取RPCFun特性的对象</param>
        /// <returns>返回获取到带有RPCFun特性的所有公开方法</returns>
        public static List<NetDelegate> GetRPCFuns(object target)
        {
            List<NetDelegate> RPCFuns = new List<NetDelegate>();
            foreach (MethodInfo info in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var rpc = info.GetCustomAttribute<RPCFun>();
                if (rpc != null)
                {
                    RPCFuns.Add(new NetDelegate(target, info, rpc.cmd));
                }
            }
            return RPCFuns;
        }
    }
}