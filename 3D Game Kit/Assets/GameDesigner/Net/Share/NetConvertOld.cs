namespace Net.Share
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// 旧版网络转换，字符串转换
    /// </summary>
    public class NetConvertOld : NetConvertBase
    {
        /// <summary> 
        /// 函数名 和 参数 与 命令之间的拆分行
        /// </summary>
        public static string Row { get; set; } = "</R>";
        /// <summary>
        /// 参数类型 和 参数值 之间的拆分修饰符
        /// </summary>
        public static string Space { get; set; } = "</S>";
        /// <summary>
        /// 数组类型 与 数组值 之间的拆分修饰符
        /// </summary>
        public static string ArraySpace { get; set; } = "</AS>";
        /// <summary>
        /// 数组元素的拆分行修饰符
        /// </summary>
        public static string ArrayRow { get; set; } = "</AR>";

        /// <summary>
        /// 参数为复杂类时 拆分对象字段间隔或类名间隔
        /// </summary>
        public static string SplitFidle { get; set; } = "</SF>";
        /// <summary>
        /// 分离类明和字段
        /// </summary>
        public static string SplitType { get; set; } = "</ST>";
        /// <summary>
        /// 分离全部字段
        /// </summary>
        public static string SplitFidles { get; set; } = "</SFs>";

        protected static ConcurrentDictionary<string, Type> Types = new ConcurrentDictionary<string, Type>();

        /// <summary>
        /// 系列化
        /// </summary>
        /// <param name="funName">函数名</param>
        /// <param name="pars">参数</param>
        /// <returns></returns>
        public static byte[] Serialize(string funName, params object[] pars)
        {
            return GetFunToBytes(funName, pars);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="buffer">二进制数据</param>
        /// <returns></returns>
        public static object[] Deserialize(byte[] buffer)
        {
            string data = Encoding.Unicode.GetString(buffer, 0, buffer.Length);
            return GetFunParams(data);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="buffer">二进制数据</param>
        /// <param name="index">开始位置</param>
        /// <param name="count">解析长度</param>
        /// <returns></returns>
        public static object[] Deserialize(byte[] buffer, int index, int count)
        {
            string data = Encoding.Unicode.GetString(buffer, index, count);
            return GetFunParams(data);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="value">字符串数据</param>
        /// <returns></returns>
        public static object[] Deserialize(string value)
        {
            return GetFunParams(value);
        }

        /// <summary>
        /// 序列化函数到字节数组
        /// </summary>
        /// <param name="funName">RPCFun函数名称</param>
        /// <param name="pars">RPCFun函数参数</param>
        /// <returns></returns>
        public static byte[] SerializeFuncBytes(string funName, params object[] pars)
        {
            return GetFunToBytes(funName, pars);
        }

        /// <summary>
        /// 序列化函数数据到字节数组
        /// </summary>
        /// <param name="funName">RPCFun函数名</param>
        /// <param name="pars">RPCFun参数</param>
        /// <returns></returns>
        public static byte[] GetFunToBytes(string funName, params object[] pars)
        {
            return Encoding.UTF8.GetBytes(SerializeFunc(funName, pars));
        }

        /// <summary>
        /// 反序列化调用RPCFun函数
        /// </summary>
        /// <param name="target">要调用的对象</param>
        /// <param name="funData">RPCFun函数数据</param>
        public static void CallFun(object target, string[] funData)
        {
            CallFun(target, funData, 0, funData.Length);
        }

        /// <summary>
        /// 反序列化调用RPCFun函数
        /// </summary>
        /// <param name="target">要调用的对象</param>
        /// <param name="funData">RPCFun函数数据</param>
        public static void CallFun(object target, string funData)
        {
            string[] fun = funData.Split(new string[] { Row }, StringSplitOptions.RemoveEmptyEntries);
            CallFun(target, fun, 0, fun.Length);
        }

        /// <summary>
        /// 反序列化调用RPCFun函数
        /// </summary>
        /// <param name="target">要调用的对象</param>
        /// <param name="funData">RPCFun函数数据</param>
        /// <param name="offset">RPCFun函数名的位置</param>
        public static void CallFun(object target, byte[] funData, int offset)
        {
            string buffer = Encoding.Unicode.GetString(funData);
            string[] fun = buffer.Split(new string[] { Row }, StringSplitOptions.RemoveEmptyEntries);
            CallFun(target, fun, offset, fun.Length);
        }

        /// <summary>
        /// 反序列化调用RPCFun函数
        /// </summary>
        /// <param name="target">要调用的对象</param>
        /// <param name="funData">RPCFun函数数据</param>
        /// <param name="offset">RPCFun函数名的位置</param>
        public static void CallFun(object target, string[] funData, int offset)
        {
            CallFun(target, funData, offset, funData.Length);
        }

        /// <summary>
        /// 反序列化调用RPCFun函数
        /// </summary>
        /// <param name="target">要调用的对象</param>
        /// <param name="funData">RPCFun函数数据</param>
        /// <param name="offset">RPCFun函数名的位置</param> 
        public static void CallFun(object target, string funData, int offset)
        {
            string[] fun = funData.Split(new string[] { Row }, StringSplitOptions.RemoveEmptyEntries);
            CallFun(target, fun, offset, fun.Length);
        }

        /// <summary>
        /// 反序列化调用RPCFun函数
        /// </summary>
        /// <param name="target">要调用的对象</param>
        /// <param name="funData">RPCFun函数数据</param>
        /// <param name="offset">RPCFun函数名的位置</param>
        /// <param name="count">RPCFun函数与参数的数据长度</param>
        public static void CallFun(object target, byte[] funData, int offset, int count)
        {
            string buffer = Encoding.Unicode.GetString(funData);
            string[] fun = buffer.Split(new string[] { Row }, StringSplitOptions.RemoveEmptyEntries);
            CallFun(target, fun, offset, count);
        }

        /// <summary>
        /// 反序列化调用RPCFun函数
        /// </summary>
        /// <param name="target">要调用的对象</param>
        /// <param name="funData">RPCFun函数数据</param>
        /// <param name="offset">RPCFun函数名的位置</param>
        /// <param name="count">RPCFun函数与参数的数据长度</param>
        public static void CallFun(object target, string funData, int offset, int count)
        {
            string[] fun = funData.Split(new string[] { Row }, StringSplitOptions.RemoveEmptyEntries);
            CallFun(target, fun, offset, count);
        }

        /// <summary>
        /// 反序列化调用RPCFun函数
        /// </summary>
        /// <param name="target">要调用的对象</param>
        /// <param name="funData">RPCFun函数数据</param>
        /// <param name="offset">RPCFun函数名的位置</param>
        /// <param name="count">RPCFun函数与参数的数据长度</param>
        public static void CallFun(object target, string[] funData, int offset, int count)
        {
            try
            {
                var method = target.GetType().GetMethod(funData[offset], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Default);
                var pars = GetFunParams(funData, offset, count);
                method.Invoke(target, pars);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 反序列化调用RPCFun函数
        /// </summary>
        /// <param name="Delegate">RPCFun委托方法</param>
        /// <param name="funData">RPCFun函数数据</param>
        public static void CallMethod(NetDelegate Delegate, string funData)
        {
            Delegate.method.Invoke(Delegate.target, GetFunParams(funData));
        }

        /// <summary>
        /// 反序列化调用RPCFun函数
        /// </summary>
        /// <param name="Delegate">RPCFun委托方法</param>
        /// <param name="funData">RPCFun函数数据</param>
        public static void CallMethod(NetDelegate Delegate, string[] funData)
        {
            Delegate.method.Invoke(Delegate.target, GetFunParams(funData, 0, funData.Length));
        }

        /// <summary>
        /// 反序列化调用RPCFun函数
        /// </summary>
        /// <param name="Delegate">RPCFun委托方法</param>
        /// <param name="funData">RPCFun函数数据</param>
        /// <param name="offset">RPCFun函数名的位置</param>
        public static void CallMethod(NetDelegate Delegate, string[] funData, int offset)
        {
            Delegate.method.Invoke(Delegate.target, GetFunParams(funData, offset, funData.Length));
        }

        /// <summary>
        /// 反序列化调用RPCFun函数
        /// </summary>
        /// <param name="Delegate">RPCFun委托方法</param>
        /// <param name="funData">RPCFun函数数据</param>
        /// <param name="offset">RPCFun函数名的位置</param>
        /// <param name="count">RPCFun函数与参数的数据长度</param>
        public static void CallMethod(NetDelegate Delegate, string[] funData, int offset, int count)
        {
            Delegate.method.Invoke(Delegate.target, GetFunParams(funData, offset, count));
        }

        /// <summary>
        /// 反序列化获取函数参数
        /// </summary>
        /// <param name="funData">函数数据</param>
        /// <returns></returns>
        public static object[] GetFunParams(string funData) => GetFunParams(funData, 0);

        /// <summary>
        /// 反序列化获取函数参数
        /// </summary>
        /// <param name="funData">函数数据</param>
        /// <param name="offset">RPCFun函数名的位置</param>
        /// <returns></returns>
        public static object[] GetFunParams(string funData, int offset)
        {
            string[] fun = funData.Split(new string[] { Row }, StringSplitOptions.RemoveEmptyEntries);
            return GetFunParams(fun, offset, fun.Length);
        }

        /// <summary>
        /// 反序列化获取函数参数
        /// </summary>
        /// <param name="funData">函数数据</param>
        /// <param name="offset">RPCFun函数名的位置</param>
        /// <param name="count">RPCFun函数与参数的数据长度</param>
        /// <returns></returns>
        public static object[] GetFunParams(string funData, int offset, int count)
        {
            string[] fun = funData.Split(new string[] { Row }, StringSplitOptions.RemoveEmptyEntries);
            return GetFunParams(fun, offset, count);
        }

        /// <summary>
        /// 解释 : 获得应用程序当前已加载的所有程序集中查找typeName的类型
        /// </summary>
        public static Type GetType(string typeName)
        {
            //代码优化
            try
            {
                return Types[typeName];
            }
            catch
            {
                typeName = typeName.Replace("&", ""); // 反射参数的 out 标示
                typeName = typeName.Replace("*", ""); // 反射参数的 int*(指针) 标示
                typeName = typeName.Replace("[]", ""); // 反射参数的 object[](数组) 标示

                if (Types.ContainsKey(typeName))
                    return Types[typeName];
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName);
                if (type != null)
                {
                    Types.TryAdd(typeName, type);
                    return type;
                }
            }
            return null;
        }

        /// <summary>
        /// 序列化函数到字符串
        /// </summary>
        /// <param name="funName">RPCFun函数名</param>
        /// <param name="pars">RPCFun参数</param>
        /// <returns></returns>
        public static string SerializeFunc(string funName, params object[] pars)
        {
            StringBuilder builder = new StringBuilder(funName + Row);
            if (pars == null)
                return builder.ToString();
            foreach (object par in pars)
            {
                if (par == null)
                {
                    builder.Append("System.Decimal" + Space + "Null" + Row);
                    continue;
                }
                Type type = par.GetType();
                if (type.IsPrimitive | par is string | type.IsEnum | type.ToString().Contains("UnityEngine.Vector") | par is Color | par is Rect | par is Quaternion)
                {
                    builder.Append(type.ToString() + Space + par + Row);
                }
                else if (type.IsArray)
                {
                    builder.Append(type.ToString() + Space);
                    foreach (object elemValue in (Array)par)
                    {
                        if (elemValue == null)
                        {
                            builder.Append("System.Decimal" + ArraySpace + "Null" + ArrayRow);
                            continue;
                        }
                        Type elemType = elemValue.GetType();
                        if (elemType.IsPrimitive | elemValue is string | elemType.IsEnum | elemType.ToString().Contains("UnityEngine.Vector") | elemValue is Color | elemValue is Rect | elemValue is Quaternion)
                            builder.Append(elemType.ToString() + ArraySpace + elemValue + ArrayRow);
                        else
                            builder.Append(SerializeField(elemValue) + ArrayRow);
                    }
                    builder.Append(Row);
                }
                else if (type.IsGenericType)
                {
                    throw new Exception("不支持泛型集合类型序列化，请使用数组代替");
                }
                else
                {
                    builder.Append(SerializeField(par) + Row);
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// 反序列化获取函数参数
        /// </summary>
        /// <param name="funData">函数数据</param>
        /// <param name="offset">RPCFun函数名的位置</param>
        /// <param name="count">RPCFun函数与参数的数据长度</param>
        /// <returns></returns>
        public static object[] GetFunParams(string[] funData, int offset, int count)
        {
            List<object> pars = new List<object>();
            for (int i = offset + 1; i < count; i++)
            {
                try
                {
                    string[] parDatas = funData[i].Split(new string[] { Space }, StringSplitOptions.RemoveEmptyEntries);
                    object value = null;
                    if (parDatas.Length == 2)
                    {
                        string typeName = parDatas[0].TrimEnd(']', '[');
                        Type type = GetType(typeName);
                        if (parDatas[0].EndsWith("]"))
                        {
                            string[] arrayDatas = parDatas[1].Split(new string[] { ArrayRow }, StringSplitOptions.RemoveEmptyEntries);
                            Array array = Array.CreateInstance(type, arrayDatas.Length);
                            for (int j = 0; j < arrayDatas.Length; j++)
                            {
                                string[] arrayValues = arrayDatas[j].Split(new string[] { ArraySpace }, StringSplitOptions.RemoveEmptyEntries);
                                object arrayValue = null;
                                if (arrayValues.Length >= 2)
                                    arrayValue = StringToValue(arrayValues[0], arrayValues[1]);
                                else
                                    arrayValue = DeserializeField(arrayValues[0]);
                                array.SetValue(arrayValue, j);
                            }
                            value = array;
                        }
                        else if (type.IsValueType | type == typeof(string))//基元结构体
                        {
                            value = StringToValue(parDatas[0], parDatas[1]);
                        }

                    }
                    else if (parDatas.Length == 1)//自定义结构或类
                    {
                        value = DeserializeField(parDatas[0]);
                    }
                    pars.Add(value);
                }
                catch
                {
                    pars.Add(null);
                }
            }
            return pars.ToArray();
        }

        /// <summary>
        /// 系列化对象字段转字符串
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <returns></returns>
        public static string SerializeField(object obj)
        {
            if (obj == null)
                return "System.Decimal" + SplitType + ArraySpace + "Null";
            Type type = obj.GetType();
            StringBuilder builder = new StringBuilder(type.ToString() + SplitType);
            foreach (FieldInfo field in type.GetFields())
            {
                try
                {
                    if (field.FieldType.IsValueType | field.FieldType == typeof(string))
                    {
                        object value = field.GetValue(obj);
                        if (value == null)
                            continue;
                        builder.Append(field.Name + SplitFidle + field.FieldType.ToString() + SplitFidle + value + SplitFidles);
                    }
                }
                catch
                {
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// 反序列化字段到新的对象
        /// </summary>
        /// <param name="data">函数数据</param>
        /// <returns></returns>
        public static T DeserializeField<T>(string data) where T : class
        {
            return DeserializeField(data) as T;
        }

        /// <summary>
        /// 反序列化字段到新的对象
        /// </summary>
        /// <param name="data">函数数据</param>
        /// <returns></returns>
        public static object DeserializeField(string data)
        {
            string[] strArray = data.Split(new string[] { SplitType }, StringSplitOptions.RemoveEmptyEntries);
            Type type = GetType(strArray[0]);
            object target = Activator.CreateInstance(type);
            string[] fidles = strArray[1].Split(new string[] { SplitFidles }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in fidles)
            {
                try
                {
                    string[] fidleValue = str.Split(new string[] { SplitFidle }, StringSplitOptions.None);
                    FieldInfo field = type.GetField(fidleValue[0]);
                    field.SetValue(target, StringToValue(fidleValue[1], fidleValue[2]));
                } catch { }
            }
            return target;
        }
    }
}
