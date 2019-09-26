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
        /// 函数数据
        /// </summary>
        public struct FuncData
        {
            /// <summary>
            /// 函数名称
            /// </summary>
            public string func;
            /// <summary>
            /// 参数数组
            /// </summary>
            public object[] pars;

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="func"></param>
            /// <param name="pars"></param>
            public FuncData(string func, object[] pars)
            {
                this.func = func;
                this.pars = pars;
            }
        }
        
        private static Dictionary<string, Type> Types = new Dictionary<string, Type>();

        /// <summary>
        /// 添加系列化类型,  当复杂类型时,如果不进行添加则系列化失败: 主要类型 Dictionary
        /// </summary>
        public static void AddSerializeType<T>()
        {
            AddSerializeType(typeof(T));
        }

        /// <summary>
        /// 添加系列化类型,  当复杂类型时,如果不进行添加则系列化失败: 主要类型 Dictionary
        /// </summary>
        /// <param name="type"></param>
        public static void AddSerializeType(Type type)
        {
            string typeString = type.ToString();
            if (!Types.ContainsKey(typeString))
                Types.Add(typeString, type);
        }

        /// <summary>
        /// 解释 : 获得应用程序当前已加载的所有程序集中查找typeName的类型
        /// </summary>
        public static Type GetType(string typeName)
        {
            //代码优化
            if (Types.ContainsKey(typeName))
                return Types[typeName];
            
            typeName = typeName.Replace("&", ""); // 反射参数的 out 标示
            typeName = typeName.Replace("*", ""); // 反射参数的 int*(指针) 标示
            typeName = typeName.Replace("[]", ""); // 反射参数的 object[](数组) 标示

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName);
                if (type != null)
                {
                    Types.Add(typeName, type);
                    return type;
                }
            }
            throw new Exception($"找不到类型:{typeName}, 类型太复杂时需要使用 NetConvertOld.AddSerializeType(type) 添加类型后再进行系列化!");
        }
        

        /// <summary>
        /// 反序列化基本类型
        /// </summary>
        /// <returns></returns>
        private static object ToBaseValue(Type type, string[] buffer, ref int index)
        {
            object obj = null;
            switch (type.ToString())
            {
                case "System.Int32":
                    obj = Convert.ToInt32(buffer[index]);
                    index++;
                    break;
                case "System.Single":
                    obj = Convert.ToSingle(buffer[index]);
                    index++;
                    break;
                case "System.Boolean":
                    obj = Convert.ToBoolean(buffer[index]);
                    index++;
                    break;
                case "System.Char":
                    obj = Convert.ToChar(buffer[index]);
                    index++;
                    break;
                case "System.Int16":
                    obj = Convert.ToInt16(buffer[index]);
                    index++;
                    break;
                case "System.Int64":
                    obj = Convert.ToInt64(buffer[index]);
                    index++;
                    break;
                case "System.UInt16":
                    obj = Convert.ToUInt16(buffer[index]);
                    index++;
                    break;
                case "System.UInt32":
                    obj = Convert.ToUInt32(buffer[index]);
                    index++;
                    break;
                case "System.UInt64":
                    obj = Convert.ToUInt64(buffer[index]);
                    index++;
                    break;
                case "System.Double":
                    obj = Convert.ToDouble(buffer[index]);
                    index++;
                    break;
                case "System.Byte":
                    obj = Convert.ToByte(buffer[index]);
                    index++;
                    break;
                case "System.SByte":
                    obj = Convert.ToSByte(buffer[index]);
                    index++;
                    break;
            }
            return obj;
        }

        /// <summary>
        /// 序列化数组实体
        /// </summary>
        private static void WriteArray(ref StringBuilder stream, Array array)
        {
            AppendLine(ref stream, array.Length.ToString());//写入数组长度
            foreach (object arr in array)
            {
                if (arr == null)//如果数组值为空
                {
                    AppendLine(ref stream, "空值");//写入0代表空值
                    continue;
                }
                AppendLine(ref stream, "有值");//写入1代表有值

                Type type = arr.GetType();
                if (type.IsPrimitive)//基本类型
                {
                    AppendLine(ref stream, type.ToString());//写入类型索引
                    AppendLine(ref stream, arr.ToString());//写入值
                }
                else if (type.IsEnum)//枚举类型
                {
                    AppendLine(ref stream, type.ToString());//写入类型索引
                    AppendLine(ref stream, arr.ToString());//写入值
                }
                else if (type == typeof(string))//字符串类型
                {
                    AppendLine(ref stream, type.ToString());//写入类型索引
                    AppendLine(ref stream, arr.ToString());//写入字符串
                }
                else if (type.IsArray)//数组类型
                {
                    Array array1 = (Array)arr;
                    AppendLine(ref stream, type.ToString());//写入类型索引
                    AppendLine(ref stream, array1.Length.ToString());//写入数组长度
                    WriteArray(ref stream, array1);//写入值
                }
                //else if (serializeTypes.Contains(type))//如果是序列化类型才进行序列化
                else
                {
                    AppendLine(ref stream, type.ToString());//写入参数类型索引
                    WriteObject(ref stream, type, arr);
                }
            }
        }

        /// <summary>
        /// 反序列化数组
        /// </summary>
        private static Array ToArray(string[] buffer, ref int index, Type type)
        {
            var arrCount = Convert.ToInt16(buffer[index]);
            index++;
            Array array = Array.CreateInstance(type, arrCount);
            ToArray(buffer, ref index, ref array);
            return array;
        }

        /// <summary>
        /// 反序列化数组
        /// </summary>
        private static void ToArray(string[] buffer, ref int index, ref Array array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                index++;
                if (buffer[index - 1] == "空值")
                    continue;

                var type = GetType(buffer[index]);
                index++;
                if (type.IsPrimitive)
                {
                    array.SetValue(ToBaseValue(type, buffer, ref index), i);
                }
                else if (type.IsEnum)
                {
                    array.SetValue(Enum.Parse(type, buffer[index]), i);
                    index++;
                }
                else if (type == typeof(string))
                {
                    array.SetValue(buffer[index], i);
                    index++;
                }
                //else if (serializeTypes.Contains(type))
                else
                {
                    array.SetValue(ToObject(buffer, ref index, type), i);
                }
            }
        }

        /// <summary>
        /// 反序列化枚举类型
        /// </summary>
        private static object ToEnum(string[] buffer, ref int index)
        {
            var type = GetType(buffer[index]);
            object obj = Enum.Parse(type, buffer[index]);
            index++;
            return obj;
        }

        static void AppendLine(ref StringBuilder stream, string str)
        {
            stream.Append(str + "\n");
        }

        /// <summary>
        /// 新版网络序列化
        /// </summary>
        /// <param name="pars"></param>
        /// <returns></returns>
        public static string Serialize(params object[] pars)
        {
            return Serialize(string.Empty, pars);
        }

        /// <summary>
        /// 新版网络序列化
        /// </summary>
        /// <param name="funcName">函数名</param>
        /// <param name="pars">参数</param>
        /// <returns></returns>
        public static string Serialize(string funcName, params object[] pars)
        {
            StringBuilder stream = new StringBuilder();
            try
            {
                AppendLine(ref stream, funcName);//写入函数名
                if (pars == null)
                    return stream.ToString();
                foreach (object par in pars)
                {
                    Type type = par.GetType();
                    if (type.IsPrimitive)//基本类型
                    {
                        AppendLine(ref stream, "基本类型");//记录类型为基类
                        AppendLine(ref stream, type.ToString());//写入类型索引
                        AppendLine(ref stream, par.ToString());//写入值
                    }
                    else if (type.IsEnum)//枚举类型
                    {
                        AppendLine(ref stream, "枚举类型");//记录类型为枚举类
                        AppendLine(ref stream, type.ToString());//写入类型索引
                        AppendLine(ref stream, par.ToString());//写入值
                    }
                    else if (type.IsArray)//数组类型
                    {
                        AppendLine(ref stream, "数组类型");//记录类型为数组
                        AppendLine(ref stream, type.ToString());//写入类型索引
                        WriteArray(ref stream, (Array)par);//写入值
                    }
                    else if (par is string)//字符串类型
                    {
                        AppendLine(ref stream, "字符串类型");//记录类型为字符串
                        AppendLine(ref stream, type.ToString());//写入类型索引
                        AppendLine(ref stream, par.ToString());//写入字符串
                    }
                    //else if (serializeTypes.Contains(type))//序列化的类型
                    else
                    {
                        if (type.IsGenericType)//泛型类型 只支持List
                        {
                            AppendLine(ref stream, "泛型");//记录类型为泛型
                            AppendLine(ref stream, type.ToString());//写入类型索引
                            if (type.ToString().Contains("Dictionary"))
                            {
                                object dicKeys = type.GetProperty("Keys").GetValue(par);
                                Type keyType = dicKeys.GetType();
                                int count = (int)keyType.GetProperty("Count").GetValue(dicKeys);
                                Array keys = Array.CreateInstance(type.GenericTypeArguments[0], count);
                                keyType.GetMethod("CopyTo").Invoke(dicKeys, new object[] { keys, 0 });
                                object dicValues = type.GetProperty("Values").GetValue(par);
                                Type valuesType = dicValues.GetType();
                                Array values = Array.CreateInstance(type.GenericTypeArguments[1], count);
                                valuesType.GetMethod("CopyTo").Invoke(dicValues, new object[] { values, 0 });
                                WriteArray(ref stream, keys);
                                WriteArray(ref stream, values);
                                continue;
                            }
                            Array array = (Array)type.GetMethod("ToArray").Invoke(par, null);
                            WriteArray(ref stream, array);
                            continue;
                        }
                        AppendLine(ref stream, "自定义类型");//记录类型为自定义类
                        AppendLine(ref stream, type.ToString());//写入类型索引
                        WriteObject(ref stream, type, par);//写入实体类型
                    }
                }
            }
            finally { }
            return stream.ToString();
        }

        /// <summary>
        /// 序列化实体类型
        /// </summary>
        private static void WriteObject(ref StringBuilder stream, Type type, object target)
        {
            var cons = type.GetConstructors();
            if (cons.Length == 0)
                return;
            if (cons[0].GetParameters().Length > 0 & !type.IsValueType)
                return;
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                var value = fields[i].GetValue(target);
                if (value == null)//如果实体字段为空，不记录
                    continue;

                if (fields[i].FieldType.IsPrimitive)//如果是基础类型
                {
                    AppendLine(ref stream, i.ToString());//写入字段索引
                    AppendLine(ref stream, value.ToString());//写入字段值
                }
                else if (fields[i].FieldType.IsEnum)//枚举类型
                {
                    AppendLine(ref stream, i.ToString());//写入字段索引
                    AppendLine(ref stream, value.ToString());//写入字段值
                }
                else if (fields[i].FieldType == typeof(string))//字符串类型
                {
                    AppendLine(ref stream, i.ToString());//写入字段索引
                    AppendLine(ref stream, value.ToString());//写入字段值
                }
                else if (fields[i].FieldType.IsArray)//如果是数组
                {
                    Array array = value as Array;
                    AppendLine(ref stream, i.ToString());//写入字段索引
                    AppendLine(ref stream, fields[i].FieldType.ToString());//写入数组类型
                    WriteArray(ref stream, array);
                }
                //else if (serializeTypes.Contains(fields[i].FieldType))//如果是序列化类型才进行序列化
                else
                {
                    AppendLine(ref stream, i.ToString());//写入字段索引
                    if (fields[i].FieldType.IsGenericType)//泛型类型 只支持List泛型类型
                    {
                        if (fields[i].FieldType.ToString().Contains("Dictionary"))
                        {
                            object dicKeys = fields[i].FieldType.GetProperty("Keys").GetValue(value);
                            Type keyType = dicKeys.GetType();
                            int count = (int)keyType.GetProperty("Count").GetValue(dicKeys);
                            Array keys = Array.CreateInstance(fields[i].FieldType.GenericTypeArguments[0], count);
                            keyType.GetMethod("CopyTo").Invoke(dicKeys, new object[] { keys, 0 });
                            object dicValues = fields[i].FieldType.GetProperty("Values").GetValue(value);
                            Type valuesType = dicValues.GetType();
                            Array values = Array.CreateInstance(fields[i].FieldType.GenericTypeArguments[1], count);
                            valuesType.GetMethod("CopyTo").Invoke(dicValues, new object[] { values, 0 });
                            WriteArray(ref stream, keys);
                            WriteArray(ref stream, values);
                            continue;
                        }
                        Array array = (Array)fields[i].FieldType.GetMethod("ToArray").Invoke(value, null);
                        WriteArray(ref stream, array);
                        continue;
                    }
                    WriteObject(ref stream, fields[i].FieldType, value);
                }
            }
            AppendLine(ref stream, "字段结束");//写入字段结束值
        }

        /// <summary>
        /// 新版反序列化
        /// </summary>
        public static FuncData Deserialize(string buffer)
        {
            FuncData value = new FuncData();
            Deserialize(buffer, (func, pars) => {
                value = new FuncData(func, pars);
            });
            return value;
        }

        /// <summary>
        /// 新版反序列化
        /// </summary>
        public static void Deserialize(string buffer, Action<string, object[]> func)
        {
            string[] buffer1 = buffer.Split(new string[0], StringSplitOptions.None);
            Deserialize(buffer1, 0, buffer1.Length, func);
        }

        /// <summary>
        /// 新版反序列化
        /// </summary>
        public static FuncData Deserialize(string[] buffer, int index, int count)
        {
            FuncData value = new FuncData();
            Deserialize(buffer, index, count, (func, pars) =>
            {
                value = new FuncData(func, pars);
            });
            return value;
        }

        /// <summary>
        /// 新版反序列化
        /// </summary>
        public static void Deserialize(string[] buffer, int index, int count, Action<string, object[]> func)
        {
            List<object> objs = new List<object>();
            var funcName = buffer[index];
            index++;
            while (index < count - 1)
            {
                var pro = buffer[index];
                index++;
                var type = GetType(buffer[index]);
                if (type == null)
                    break;
                index++;
                switch (pro)
                {
                    case "基本类型":
                        objs.Add(ToBaseValue(type, buffer, ref index));
                        break;
                    case "枚举类型":
                        objs.Add(Enum.Parse(type, buffer[index]));
                        index++;
                        break;
                    case "数组类型":
                        var arrCount = Convert.ToInt32(buffer[index]);
                        index++;
                        Array array = Array.CreateInstance(type, arrCount);
                        ToArray(buffer, ref index, ref array);
                        objs.Add(array);
                        break;
                    case "字符串类型":
                        objs.Add(buffer[index]);
                        index++;
                        break;
                    case "泛型":
                        var arrCount1 = Convert.ToInt32(buffer[index]);
                        index++;
                        object list = Activator.CreateInstance(type);
                        if (type.ToString().Contains("Dictionary"))
                        {
                            Type dicType = list.GetType();
                            Type keysT = type.GenericTypeArguments[0];
                            Type valuesT = type.GenericTypeArguments[1];
                            Array keys = Array.CreateInstance(keysT, arrCount1);
                            Array values = Array.CreateInstance(valuesT, arrCount1);
                            ToArray(buffer, ref index, ref keys);
                            index++;
                            ToArray(buffer, ref index, ref values);
                            for (int i = 0; i < keys.Length; i++)
                            {
                                dicType.GetMethod("Add").Invoke(list, new object[] { keys.GetValue(i), values.GetValue(i) });
                            }
                        }
                        else
                        {
                            Type itemType = type.GenericTypeArguments[0];
                            Array array1 = Array.CreateInstance(itemType, arrCount1);
                            ToArray(buffer, ref index, ref array1);
                            var met = type.GetMethod("AddRange");
                            met.Invoke(list, new object[] { array1 });
                        }
                        objs.Add(list);
                        break;
                    case "自定义类型":
                        var obj1 = ToObject(buffer, ref index, type);
                        objs.Add(obj1);
                        break;
                }
            }
            func(funcName, objs.ToArray());
        }

        /// <summary>
        /// 反序列化实体对象
        /// </summary>
        private static object ToObject(string[] buffer, ref int index, Type type)
        {
            var cons = type.GetConstructors();
            if (cons.Length == 0)
                return null;
            if (cons[0].GetParameters().Length > 0 & !type.IsValueType)
                return null;
            object obj = obj = Activator.CreateInstance(type);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                if (buffer[index] == "字段结束")//字段结束符
                    break;

                if (i.ToString() != buffer[index])//如果字段匹配不对，跳过
                    continue;

                index++;

                if (fields[i].FieldType.IsPrimitive)//如果是基础类型
                {
                    fields[i].SetValue(obj, ToBaseValue(fields[i].FieldType, buffer, ref index));
                }
                else if (fields[i].FieldType.IsEnum)//如果是枚举类型
                {
                    fields[i].SetValue(obj, Enum.Parse(fields[i].FieldType, buffer[index]));
                    index++;
                }
                else if (fields[i].FieldType == typeof(string))//如果是字符串
                {
                    fields[i].SetValue(obj, buffer[index]);
                    index++;
                }
                else if (fields[i].FieldType.IsArray)//如果是数组类型
                {
                    var arrType = GetType(buffer[index]);
                    index++;
                    var arrCount = Convert.ToInt32(buffer[index]);
                    index++;
                    Array array = Array.CreateInstance(arrType, arrCount);
                    ToArray(buffer, ref index, ref array);
                    fields[i].SetValue(obj, array);
                }
                //else if (serializeTypes.Contains(fields[i].FieldType))//如果是序列化类型
                else
                {
                    if (fields[i].FieldType.IsGenericType)
                    {
                        if (fields[i].FieldType.GenericTypeArguments.Length == 2)
                            fields[i].SetValue(obj, ToDictionary(buffer, ref index, fields[i].FieldType));
                        else
                            fields[i].SetValue(obj, ToList(buffer, ref index, fields[i].FieldType));
                        continue;
                    }
                    fields[i].SetValue(obj, ToObject(buffer, ref index, fields[i].FieldType));
                }
            }
            index++;
            return obj;
        }

        /// <summary>
        /// 反序列化泛型
        /// </summary>
        private static object ToList(string[] buffer, ref int index, Type type)
        {
            var arrCount1 = Convert.ToInt16(buffer[index]);
            index++;
            object list = Activator.CreateInstance(type);
            Type itemType = type.GenericTypeArguments[0];
            Array array1 = Array.CreateInstance(itemType, arrCount1);
            ToArray(buffer, ref index, ref array1);
            var met = type.GetMethod("AddRange");
            met.Invoke(list, new object[] { array1 });
            return list;
        }

        /// <summary>
        /// 反序列化字典类型
        /// </summary>
        private static object ToDictionary(string[] buffer, ref int index, Type type)
        {
            var arrCount1 = Convert.ToInt16(buffer[index]);
            index++;
            object dic = Activator.CreateInstance(type);
            Type keysT = type.GenericTypeArguments[0];
            Type valuesT = type.GenericTypeArguments[1];
            Array keys = Array.CreateInstance(keysT, arrCount1);
            Array values = Array.CreateInstance(valuesT, arrCount1);
            ToArray(buffer, ref index, ref keys);
            index++;
            ToArray(buffer, ref index, ref values);
            for (int i = 0; i < keys.Length; i++)
            {
                type.GetMethod("Add").Invoke(dic, new object[] { keys.GetValue(i), values.GetValue(i) });
            }
            return dic;
        }
    }
}