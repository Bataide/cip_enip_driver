// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Class: CIPSerializer
// Description: CIP message serializer
// -----------------------------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Techsteel.Drivers.CIP
{
    public class CIPSerializer
    {
        public ushort SizeOf()
        {
            return SizeOf(this);
        }

        private ushort SizeOf(object obj)
        {
            string name = obj.GetType().Name;
            ushort size = 0;
            foreach (FieldInfo fi in obj.GetType().GetFields())
            {
                ushort lastSize = size;

                Type fiType = fi.FieldType;
                object objField = fi.GetValue(obj);
                if (objField != null)
                    if (fiType.IsEnum)
                    {
                        size += (ushort)Marshal.SizeOf(Enum.GetUnderlyingType(fiType));
                    }
                    else if (fiType.IsPrimitive)
                    {
                        size += SizeOfPrimitive(objField);
                    }
                    else if (fiType.IsArray)
                    {
                        foreach (object objElem in (Array)objField)
                            if (objElem != null)
                            {
                                Type objElemType = objElem.GetType();
                                if (objElemType.IsPrimitive)
                                    size += SizeOfPrimitive(objElem);
                                else if (objElemType.IsClass)
                                    size += SizeOf(objElem);
                                else
                                    throw new Exception("SizeOf error");
                            }
                    }
                    else if (fiType.Name == "List`1")
                    {
                        int count = (int)objField.GetType().GetMethod("get_Count").Invoke(objField, null);
                        for (int i = 0; i < count; i++)
                        {
                            object objElem = objField.GetType().GetMethod("get_Item").Invoke(objField, new object[] { i });
                            size += SizeOf(objElem);
                        }
                    }
                    else if (fiType.Name == "Nullable`1")
                    {
                        Type objNullType = fiType.GetGenericArguments()[0];
                        size += (ushort)Marshal.SizeOf(objNullType);
                    }
                    else if (fiType.IsClass)
                    {
                        size += SizeOf(objField);
                    }
                    else
                        throw new Exception("SizeOf error");
            }
            return size;
        }

        private ushort SizeOfPrimitive(object obj)
        {
            if (obj is byte) return 1;
            if (obj is short) return 2;
            if (obj is ushort) return 2;
            if (obj is int) return 4;
            if (obj is uint) return 4;
            if (obj is long) return 8;
            if (obj is ulong) return 8;
            throw new Exception(string.Format("SizeOf error. Primitive type {0} not supported.", obj.GetType().FullName));
        }

        public byte[] Serialize()
        {
            return Serialize(this);
        }

        public byte[] Serialize(object obj)
        {
            List<byte> bytes = new List<byte>();
            foreach (FieldInfo fi in obj.GetType().GetFields())
            {
                Type fiType = fi.FieldType;
                object objField = fi.GetValue(obj);
                if (objField != null)
                    if (fiType.IsEnum)
                    {
                        Type enumType = Enum.GetUnderlyingType(fiType);
                        if (enumType == typeof(byte))
                            bytes.Add((byte)objField);
                        else if (enumType == typeof(short))
                            bytes.AddRange(BitConverter.GetBytes((short)objField));
                        else if (enumType == typeof(ushort))
                            bytes.AddRange(BitConverter.GetBytes((ushort)objField));
                        else if (enumType == typeof(int))
                            bytes.AddRange(BitConverter.GetBytes((int)objField));
                        else if (enumType == typeof(uint))
                            bytes.AddRange(BitConverter.GetBytes((uint)objField));
                        else if (enumType == typeof(long))
                            bytes.AddRange(BitConverter.GetBytes((long)objField));
                        else if (enumType == typeof(ulong))
                            bytes.AddRange(BitConverter.GetBytes((ulong)objField));
                        else
                            throw new Exception(string.Format("Unsupported type for enum: {0}", enumType.Name));
                    }
                    else if (fiType.IsPrimitive)
                    {
                        byte[] v = SerializePrimitive(objField);
                        bytes.AddRange(v);
                    }
                    else if (fiType.IsArray)
                    {
                        int i = 0;
                        foreach (object objElem in (Array)objField)
                        {
                            if (objElem != null)
                            {
                                Type objElemType = objElem.GetType();
                                if (objElemType.IsPrimitive)
                                {
                                    byte[] v = SerializePrimitive(objElem);
                                    bytes.AddRange(v);
                                }
                                else if (objElemType.IsClass)
                                    bytes.AddRange(Serialize(objElem));
                                else
                                    throw new Exception("serialize error");
                            }
                            i++;
                        }
                    }
                    else if (fiType.Name == "List`1")
                    {
                        int count = (int)objField.GetType().GetMethod("get_Count").Invoke(objField, null);
                        for (int i = 0; i < count; i++)
                        {
                            object objElem = objField.GetType().GetMethod("get_Item").Invoke(objField, new object[] { i });
                            bytes.AddRange(Serialize(objElem));
                        }
                    }
                    else if (fiType.Name == "Nullable`1")
                    {
                        byte[] v = SerializePrimitive(objField);
                        bytes.AddRange(v);
                    }
                    else if (fiType.IsClass)
                    {
                        bytes.AddRange(Serialize(objField));
                    }
                    else
                        throw new Exception("Serialize error");
            }
            return bytes.ToArray();
        }

        private byte[] SerializePrimitive(object obj)
        {
            if (obj is byte) return new byte[] { (byte)obj };
            if (obj is short) return BitConverter.GetBytes((short)obj);
            if (obj is ushort) return BitConverter.GetBytes((ushort)obj);
            if (obj is int) return BitConverter.GetBytes((int)obj);
            if (obj is uint) return BitConverter.GetBytes((uint)obj);
            if (obj is long) return BitConverter.GetBytes((long)obj);
            if (obj is ulong) return BitConverter.GetBytes((ulong)obj);
            throw new Exception(string.Format("Serialize error. Primitive type {0} not supported.", obj.GetType().FullName));
        }

        public static object Deserialize(Type type, byte[] data, ref int pointer)
        {
            MethodInfo mi = type.GetMethod("GetChildType");
            Type objType = (Type)mi?.Invoke(null, new object[] { data[pointer] }) ?? type;
            object obj = Activator.CreateInstance(objType);
            foreach (FieldInfo fi in objType.GetFields())
            {
                Type fiType = fi.FieldType;
                if (fiType.IsEnum)
                {
                    if (fiType.IsEnum)
                    {
                        Type enumType = Enum.GetUnderlyingType(fiType);
                        if (enumType == typeof(byte))
                            fi.SetValue(obj, data[pointer]);
                        else if (enumType == typeof(short))
                            fi.SetValue(obj, BitConverter.ToInt16(data, pointer));
                        else if (enumType == typeof(ushort))
                            fi.SetValue(obj, BitConverter.ToUInt16(data, pointer));
                        else if (enumType == typeof(int))
                            fi.SetValue(obj, BitConverter.ToInt32(data, pointer));
                        else if (enumType == typeof(uint))
                            fi.SetValue(obj, BitConverter.ToUInt32(data, pointer));
                        else if (enumType == typeof(long))
                            fi.SetValue(obj, BitConverter.ToInt64(data, pointer));
                        else if (enumType == typeof(ulong))
                            fi.SetValue(obj, BitConverter.ToUInt64(data, pointer));
                        else
                            throw new Exception(string.Format("Unsupported type for enum: {0}", enumType.Name));
                        pointer += Marshal.SizeOf(enumType);
                    }
                }
                else if (fiType.IsPrimitive)
                {
                    object v = DeserializePrimitive(fiType, data, ref pointer);
                    fi.SetValue(obj, v);
                }
                else if (fiType.IsArray)
                {
                    ushort arySize = (ushort)objType.GetMethod(nameof(GetArraySize)).Invoke(obj, new object[] { fi.Name });
                    Type objElemType = fiType.GetElementType();
                    object objField = Array.CreateInstance(objElemType, arySize);
                    fi.SetValue(obj, objField);
                    for (int i = 0; i < arySize; i++)
                    {
                        if (objElemType.IsPrimitive)
                        {
                            object v = DeserializePrimitive(objElemType, data, ref pointer);
                            ((Array)objField).SetValue(v, i);
                        }
                        else if (objElemType.IsClass)
                        {
                            ((Array)objField).SetValue(Deserialize(objElemType, data, ref pointer), i);
                        }
                        else
                            throw new Exception("deserialize error");
                    }
                }
                else if (fiType.Name == "List`1")
                {
                    Type objElemType = fiType.GetGenericArguments()[0];
                    object objField = Activator.CreateInstance((typeof(List<>).MakeGenericType(objElemType)));
                    fi.SetValue(obj, objField);
                    while (true)
                    {
                        if ((bool)objType.GetMethod(nameof(IsListCompleted)).Invoke(obj, new object[] { fi.Name }))
                            break;
                        object objListItem = Deserialize(objElemType, data, ref pointer);
                        objField.GetType().GetMethod("Add").Invoke(objField, new object[] { objListItem });
                    }
                }
                else if (fiType.Name == "Nullable`1")
                {
                    if ((bool)objType.GetMethod(nameof(HasNullableFieldValue)).Invoke(obj, new object[] { fi.Name }))
                    {
                        Type nullType = fiType.GetGenericArguments()[0];
                        object v = DeserializePrimitive(nullType, data, ref pointer);
                        fi.SetValue(obj, v);
                    }
                }
                else if (fiType.IsClass)
                {
                    fi.SetValue(obj, Deserialize(fiType, data, ref pointer));
                }
                else
                    throw new Exception("Serialize error");
            }
            return obj;
        }

        private static object DeserializePrimitive(Type type, byte[] data, ref int pointer)
        {
            try
            {
                if (type == typeof(byte)) return data[pointer];
                if (type == typeof(short)) return BitConverter.ToInt16(data, pointer);
                if (type == typeof(ushort)) return BitConverter.ToUInt16(data, pointer);
                if (type == typeof(int)) return BitConverter.ToInt32(data, pointer);
                if (type == typeof(uint)) return BitConverter.ToUInt32(data, pointer);
                if (type == typeof(long)) return BitConverter.ToInt64(data, pointer);
                if (type == typeof(ulong)) return BitConverter.ToUInt64(data, pointer);
                throw new Exception(string.Format("Deserialize error. Primitive type {0} not supported.", type.FullName));
            }
            finally
            {
                pointer += Marshal.SizeOf(type);
            }
        }

        public virtual ushort GetArraySize(string fieldName)
        {
            return 0;
        }

        public virtual bool IsListCompleted(string fieldName)
        {
            return true;
        }

        public virtual bool HasNullableFieldValue(string fieldName)
        {
            return false;
        }
    }
}
