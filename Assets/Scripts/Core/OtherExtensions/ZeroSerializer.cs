// Created by ZeroAs on 12/10/2023
//自用序列化小工具，不喜勿喷。可以装MessagePack.Unity包，然后把下面那俩resolver注释给打开。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.ImmutableCollection;
using MessagePack.Resolvers;
namespace ZeroAs.Serialization
{
    public static class ZeroSerializer
    {
        public static MessagePackSerializerOptions option;

        static ZeroSerializer(){
            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                //MessagePack.Unity.Extension.UnityBlitResolver.Instance,
                //MessagePack.Unity.UnityResolver.Instance,
                StandardResolver.Instance
            );
            option = new MessagePackSerializerOptions(resolver).WithCompression(MessagePackCompression.None);
        }

        const bool UseLog = false;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log(object obj) { 
            if(UseLog)
            Debug.Log(obj);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogWarning(object obj)
        {
            if (UseLog)
                Debug.LogWarning(obj);
        }
        static Regex 不可进行备份的正则匹配 = new Regex("^(UnityEngine\\.(Editor))", RegexOptions.Compiled);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Serialize(object Urobject) //序列化 返回byte[]类型
        {
            if (Urobject == null)
            {
                Log("The Object Is Null");
                return null;
            }/*
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream memory = new MemoryStream();
            bf.Serialize(memory, Urobject);
            byte[] bytes = memory.GetBuffer();
            memory.Close();
            */

            Log("Serialize Type: "+Urobject.GetType());
            return MessagePackSerializer.Serialize(Urobject.GetType(), Urobject, option);
            //return bytes;
        }
        [Serializable]
        [MessagePackObject]
        public class DictionarySerializer
        {
            [Key(0)]
            public Dictionary<string, byte[]> dict = new Dictionary<string, byte[]>();
            //public byte[,] ArrayContent;
            [Key(1)]
            public Type type;
            [Key(2)]
            public bool isArrayMode = false;
            public const string ARRAY_KEY = "arr";
        }
        class DictionarySerializerPreventLoop
        {
            public Dictionary<string, byte[]> dict;
            public string key;
            public object valueMaster;
            public DictionarySerializerPreventLoop(Dictionary<string, byte[]> dict, string key, object valueMaster)
            {
                this.dict = dict;
                this.key = key;
                this.valueMaster = valueMaster;
            }
            public override bool Equals(object obj)
            {
                return obj is DictionarySerializerPreventLoop obj2 ? Equals(obj2) : false;
            }
            public bool Equals(DictionarySerializerPreventLoop obj)
            {
                return dict == obj.dict && key == obj.key && valueMaster == obj.valueMaster;
            }
            public static bool operator ==(DictionarySerializerPreventLoop a, DictionarySerializerPreventLoop b)
            {
                return a.Equals(b);
            }
            public static bool operator !=(DictionarySerializerPreventLoop a, DictionarySerializerPreventLoop b)
            {
                return !a.Equals(b);
            }
            public override int GetHashCode()
            {
                return HashCode.Combine(dict.GetHashCode(), key.GetHashCode(), valueMaster.GetHashCode());
            }
        }
        private static bool _IsIndexer(PropertyInfo propertyInfo)
        {
            // 检查是否为索引器
            return propertyInfo.GetIndexParameters().Any();
        }
        public static byte[] SerializeObjectToBytes(object TheObject)
        {
            if (TheObject == null) { return null; }
            //防止图的循环
            Dictionary<object, byte[]> preventLoopInGraph = new Dictionary<object, byte[]>();
            HashSet<object> hasVisited = new HashSet<object>();
            Regex unableNameSpace = 不可进行备份的正则匹配;
            bool checkUnableType(object value = null)
            {
                if (value == null)
                {
                    return false;
                }
                Type type = value.GetType();
                if (type == typeof(Type))
                {
                    type = (Type)value;
                }
                if (type.Namespace != null && unableNameSpace.IsMatch(type.Namespace))
                {
                    return false;
                }
                if (type == typeof(GameObject) || type.IsSubclassOf(typeof(GameObject)))
                {
                    return false;
                }
                return true;
            }
            void TryGetElseLoop(Dictionary<string, byte[]> state, string key, object value = null)
            {
                if (!checkUnableType(value))
                {
                    Log("Unable To Backup " + key + " because parent type is unsupported or parent is null");
                    return;
                }
                if (preventLoopInGraph.ContainsKey(value))
                {
                    state[key] = preventLoopInGraph[value];
                }
                else if (!hasVisited.Contains(value))
                {
                    state[key] = dfs(value);
                }
                else
                {
                    Log("Unable To Backup " + key + " because loop detected");
                }
            }
            if (!checkUnableType(TheObject))
            {
                throw new SerializationException("Object type isnt supported.");
            }
            byte[] dfs(object Urobject) //序列化 返回byte[]类型"
            {
                if (preventLoopInGraph.ContainsKey(Urobject))
                {
                    return preventLoopInGraph[Urobject];
                }
                hasVisited.Add(Urobject);
                byte[] bytes;
                try
                {
                    if (Urobject is MonoBehaviour)
                    {
                        Log("Trying To Backup Monobehaviour");
                        throw new SerializationException("Monobehaviour type is supported onlu Dictionary.");
                    }
                    else
                    {
                        bytes = Serialize(Urobject);
                    }
                }
                catch (MessagePackSerializationException e)
                {
                    Log("Try To Use Dictionary Serilization Because: " + e);
                    DictionarySerializer();
                }
                catch (SerializationException e)
                {
                    Log("Try To Use Dictionary Serilization Because: " + e);
                    DictionarySerializer();
                }
                catch (TargetInvocationException tie)
                {
                    // 检查内部异常是否是 MessagePackSerializationException
                    if (tie.InnerException is MessagePackSerializationException innerEx)
                    {
                        Log(
                            "Serialization failed via TargetInvocationException, Try To Use Dictionary Serilization Because: " +
                            tie);
                        DictionarySerializer();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception e)
                {
                    Log("I Hate You "+e);
                    return null;
                }
                void DictionarySerializer()
                {
                    Type type = Urobject.GetType();
                    DictionarySerializer dictionarySerilizer = new DictionarySerializer();
                    FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    PropertyInfo[] ppts = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        .Where(p => p.CanRead && p.CanWrite)
                        .ToArray();
                    Dictionary<string, byte[]> state = dictionarySerilizer.dict;
                    dictionarySerilizer.type = type;
                    if (type.IsSubclassOf(typeof(Array)))
                    {
                        Log("backup array " + type);
                        if (!checkUnableType(type.GetElementType()))
                        {
                            throw new SerializationException("Object type isnt supported.");
                        }
                        //object[] arr = (object[])Urobject;
                        Array lst = Urobject as Array;
                        Log("len: "+lst.Length);
                        byte[][] bytesArr = new byte[lst.Length][];
                        for (int i = 0; i < lst.Length; i++)
                        {
                            object tmpVal = lst.GetValue(i);
                            if (tmpVal==null) {
                                bytesArr[i] = null;
                                continue;
                            }
                            bytesArr[i] = dfs(tmpVal);//每个都深度遍历
                        }
                        bytes = Serialize(bytesArr);//做两次序列化
                        dictionarySerilizer.isArrayMode = true;
                        dictionarySerilizer.dict[ZeroSerializer.DictionarySerializer.ARRAY_KEY] = bytes;
                        bytes = Serialize(dictionarySerilizer);
                    }
                    else
                    {
                        foreach (FieldInfo field in fields)
                        {
                            if (
                                field.GetCustomAttribute<NonSerializedAttribute>() != null ||
                                field.GetCustomAttribute<DontSerializedWhenDictionarySerializing>() != null
                            )
                            {
                                Log("jump: " + field.Name);
                                continue;
                            }
                            Log("backup " + field.Name);
                            //如果有值则直接添加，已经开始遍历没有则进行等待，如果还没遍历过则遍历
                            TryGetElseLoop(state, field.Name, field.GetValue(Urobject));
                        }
                        foreach (PropertyInfo field in ppts)
                        {
                            if (
                                field.GetCustomAttribute<NonSerializedAttribute>() != null ||
                                field.GetCustomAttribute<DontSerializedWhenDictionarySerializing>() != null ||
                                _IsIndexer(field)         //如果是索引器也不备份，因为索引器通常是从其他地方取值的
                            )
                            {
                                Log("jump: " + field.Name);
                                continue;
                            }
                            try
                            {
                                //如果有值则直接添加，已经开始遍历没有则进行等待，如果还没遍历过则遍历
                                TryGetElseLoop(state, field.Name, field.GetValue(Urobject));
                            }
                            catch (Exception ex)
                            {
                                state.Remove(field.Name);
                                Log("unable to backup: " + field.Name + " because " + ex.Message);
                                continue;
                            }
                        }
                        bytes = Serialize(dictionarySerilizer);
                    }
                }
                //设置值
                preventLoopInGraph.Add(Urobject, bytes);
                return bytes;
            }
            return dfs(TheObject);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Deserialize(byte[] bytes,Type type) //反序列化，返回object类型的
        {
            /*BinaryFormatter bf = new BinaryFormatter();
            bf.Binder = new SafeSerializationBinder(type);
            MemoryStream memory = new MemoryStream(bytes);
            object ss = bf.Deserialize(memory);
            memory.Close();
            return ss;*/
            try
            {
                return MessagePackSerializer.Deserialize(type, bytes, option);
            }
            catch(MessagePackSerializationException e)
            {
                Log("Try to use DictionarySerializer to deserialize.");
                //不是这个就一定是DictionarySerializer
                return MessagePackSerializer.Deserialize(typeof(DictionarySerializer), bytes, option);
            }catch (TargetInvocationException tie)
            {
                // 检查内部异常是否是 MessagePackSerializationException
                if (tie.InnerException is MessagePackSerializationException innerEx)
                {
                    Log(
                        "Deserialization failed via TargetInvocationException, Try To Use Dictionary to deserialize " +
                        tie);
                    return MessagePackSerializer.Deserialize(typeof(DictionarySerializer), bytes, option);
                }
                else
                {
                    throw;
                }
            }
            catch (Exception e)
            {
                Log("I Hate You "+e);
                return null;
            }
        }
        [Serializable]
        class obj_receiver<T>
        {
            public T obj;//这个名字不能改，会在下面反射用到
        }
        public static void DeserializeBytesToObject(byte[] obj,ref object recoverTarget) {
            object theObject;
            try
            {
                theObject = Deserialize(obj,recoverTarget.GetType());
            }
            catch (SecurityException e)
            {
                Debug.LogError("格式错误，可能遭到文件攻击");
                {
                    return;
                }
            }
            Type theType = theObject.GetType();
            if (theType == typeof(DictionarySerializer)) {
                Type generic = typeof(obj_receiver<>);
                generic = generic.MakeGenericType(recoverTarget.GetType());
                object receiver = Activator.CreateInstance(generic);
                FieldInfo subinfo = generic.GetField("obj");
                subinfo.SetValue(receiver, recoverTarget);
                DeserializeCopyToObject(subinfo, receiver, obj);
            }
            else
            {
                recoverTarget = theObject;
            }
        }
        public static void DeserializeCopyToObject(MemberInfo info, object obj, byte[] bytes = null)
        {
            FieldInfo fd = null;
            PropertyInfo ppt = null;
            Type wantType = null;
            if (info is FieldInfo)
            {
                fd = (FieldInfo)info;
                wantType = fd.FieldType;
            }
            else if (info is PropertyInfo)
            {
                ppt = (PropertyInfo)info;
                wantType = ppt.PropertyType;
            }
            else
            {
                return;
            }
            object ret;
            try
            {
                ret = Deserialize(bytes, wantType);
            }
            catch (SecurityException e)
            {
                Debug.LogError("格式错误，可能遭到文件攻击");
                {
                    return;
                }
            }
            Type type = ret.GetType();//可能是普通wantType也可能是DictionarySerializer
            if (type == typeof(DictionarySerializer))
            {
                Log("NonSerializeAbleObject Recovering");
                //return null;
                DictionarySerializer deserializer = (DictionarySerializer)ret;
                if (deserializer.isArrayMode)
                {
                    manufactureArrayMode((byte[][])Deserialize(deserializer.dict[DictionarySerializer.ARRAY_KEY],typeof(byte[][])));
                }
                else
                {
                    object subObj = null;
                    if (info is FieldInfo)
                    {
                        subObj = fd.GetValue(obj);
                    }
                    else if (info is PropertyInfo)
                    {
                        subObj = ppt.GetValue(obj);
                    }
                    Type subType = deserializer.type;
                    if (subObj == null)
                    {
                        //monobehaviour必须要已经有组件才能重写
                        if (subType.IsSubclassOf(typeof(MonoBehaviour)))
                        {
                            LogWarning("Unable To Recover MonoBehaviour: " + subType.Name + " because not found existing monobehaviour");
                            return;
                        }
                        LogWarning("Null Object For DictionarySerializer: " + subType.Name);
                        subObj = Activator.CreateInstance(subType);
                    }
                    FieldInfo[] fields = subType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                    foreach (FieldInfo field in fields)
                    {
                        if (field.GetCustomAttribute<DontSerializedWhenDictionarySerializing>() != null)
                        {
                            Log("jump recover: " + field.Name);
                            continue;
                        }
                        if (deserializer.dict.TryGetValue(field.Name, out byte[] fieldValueBytes))
                        {
                            //Log(field.FieldType);
                            DeserializeCopyToObject(field, subObj, fieldValueBytes);
                        }
                    }
                    PropertyInfo[] ppts = subType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        .Where(p => p.CanRead && p.CanWrite)
                        .ToArray();

                    foreach (PropertyInfo field in ppts)
                    {
                        if (field.GetCustomAttribute<DontSerializedWhenDictionarySerializing>() != null)
                        {
                            Log("jump recover: " + field.Name);
                            continue;
                        }
                        if (deserializer.dict.TryGetValue(field.Name, out byte[] fieldValueBytes))
                        {
                            DeserializeCopyToObject(field, subObj, fieldValueBytes);
                        }
                    }
                    Log("recover: " + info.Name);
                    if (fd != null)
                    {
                        fd.SetValue(obj, subObj);
                    }
                    else if (ppt != null)
                    {
                        ppt.SetValue(obj, subObj);
                    }
                }
            }
            else
            {
                Log("recover: " + info.Name);
                if (fd != null)
                {
                    fd.SetValue(obj, ret);
                }
                else if (ppt != null)
                {
                    ppt.SetValue(obj, ret);
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void manufactureArrayMode(byte[][] data)
            {
                Type theThingType = fd != null ? fd.FieldType : ppt.PropertyType;
                Log(theThingType);
                Array objs = Array.CreateInstance(theThingType.GetElementType(), data.Length);
                Array oriObjsCopier = (fd != null ? fd.GetValue(obj) : ppt.GetValue(obj)) as Array;
                //复制出来的东西长度取最大值，避免List缩小/增大，oriObjs太大/太小
                Array oriObjs = Array.CreateInstance(theThingType.GetElementType(), Math.Max(oriObjsCopier?.Length??0,data.Length));
                //先复制回去，避免遇到需要用引用的对象比如Monobehaviour
                if (oriObjsCopier != null)
                {
                    for (int i = 0;i<oriObjsCopier.Length;i++) {
                        oriObjs.SetValue(oriObjsCopier.GetValue(i), i);
                    }
                }
                Type generic = typeof(obj_receiver<>);
                generic = generic.MakeGenericType(theThingType.GetElementType());
                object receiver = Activator.CreateInstance(generic);
                FieldInfo subinfo = receiver.GetType().GetField("obj");
                for (int i = 0; i < data.Length; i++)
                {
                    subinfo.SetValue(receiver, oriObjs.GetValue(i));
                    DeserializeCopyToObject(subinfo, receiver, data[i]);
                    //Debug.Log(subinfo.GetValue(receiver).GetType()+" "+objs.GetType());
                    objs.SetValue(subinfo.GetValue(receiver), i);
                }
                if (fd != null)
                {
                    fd.SetValue(obj, objs);
                }
                else if (ppt != null)
                {
                    ppt.SetValue(obj, objs);
                }
                Log("recover: " + info.Name);
            }
        }
    }
    public sealed class DontSerializedWhenDictionarySerializing : Attribute
    {
    }
}