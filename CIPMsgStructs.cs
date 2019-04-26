// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Structs
// -----------------------------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq;

namespace Techsteel.Drivers.CIP
{       
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class CommandEtherNetIPHeader
    {
        public const int SENDER_CONTEXT_SIZE = 8;
        public EncapsulationCommands Command;
        public ushort Length;
        public uint SessionHandle;
        public uint Status;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SENDER_CONTEXT_SIZE)]
        public byte[] SenderContext = new byte[SENDER_CONTEXT_SIZE];
        public uint Options;
        public static CommandEtherNetIPHeader Deserialize(byte[] buffer)
        {
            object obj = null;
            Type objType = typeof(CommandEtherNetIPHeader);
            if ((buffer != null) && (buffer.Length > 0))
            {
                IntPtr ptrObj = IntPtr.Zero;
                try
                {
                    int objSize = Marshal.SizeOf(objType);
                    if (objSize > 0)
                    {
                        if (buffer.Length < objSize)                            
                            throw new Exception(string.Format("Insufficient byte amount to deserialize 'Encapsulation Header' struct ({0} byte(s)", buffer.Length));
                        ptrObj = Marshal.AllocHGlobal(objSize);
                        if (ptrObj != IntPtr.Zero)
                        {
                            Marshal.Copy(buffer, 0, ptrObj, objSize);
                            obj = Marshal.PtrToStructure(ptrObj, objType);
                        }
                        else
                            throw new Exception("Could not allocate memory to deserialize 'Encapsulation Header' struct");
                    }
                }
                finally
                {
                    if (ptrObj != IntPtr.Zero)
                        Marshal.FreeHGlobal(ptrObj);
                }
            }
            return (CommandEtherNetIPHeader)obj;
        }
        public byte[] Serialize()
        {
            byte[] buffer = null;
            if (this != null)
            {
                Type objType = this.GetType();
                int objSize = Marshal.SizeOf(objType);
                if (objSize > 0)
                {
                    IntPtr ptrObj = IntPtr.Zero;
                    try
                    {
                        buffer = new byte[objSize];
                        ptrObj = Marshal.AllocHGlobal(objSize);
                        if (ptrObj != IntPtr.Zero)
                        {
                            Marshal.StructureToPtr(this, ptrObj, true);
                            Marshal.Copy(ptrObj, buffer, 0, buffer.Length);
                        }
                        else
                            throw new Exception("Fail to allocate memory to receive deserialization bytes");
                    }
                    finally
                    {
                        if (ptrObj != IntPtr.Zero)
                            Marshal.FreeHGlobal(ptrObj);
                    }
                    if (buffer == null)
                        throw new Exception("General failure on byte array creation during serialization");
                }
            }
            return buffer;
        }
    }

    public class CommandSpecificDataListServices : CIPSerializer
    {
        public ushort ItemCount;
        public CommandSpecificDataListServicesItem[] Items;        
        public override ushort GetArraySize(string fieldName)
        {
            if (fieldName == nameof(Items))
                return ItemCount;
            throw new Exception(string.Format("Field {0} not found in struct {1} func. {2}", fieldName, nameof(CommandSpecificDataListServices), nameof(GetArraySize)));
        }
    }

    public class CommandSpecificDataListServicesItem : CIPSerializer
    {
        public CommonPacketItemID TypeCode;
        public ushort Length;
        public ushort Version;
        public ushort CapabilityFlags;
        public byte[] ServiceName;
        public override ushort GetArraySize(string fieldName)
        {
            if (TypeCode == CommonPacketItemID.ListServicesResponse)
                if (fieldName == nameof(ServiceName))
                    return (ushort)(Length - 2 - 2);
            throw new Exception(string.Format("Field {0} not found in struct {1} func. {2}", fieldName, nameof(CommandSpecificDataListServicesItem), nameof(GetArraySize)));
        }
    }

    public class CommandSpecificDataRegisterSession : CIPSerializer
    {
        public ushort ProtocolVersion;
        public ushort OptionsFlags;
    }

    public class CommandSpecificDataSendRRData : CIPSerializer
    {
        public uint InterfaceHandle;
        public ushort Timeout;
        public ushort ItemCount;
        public CommandSpecificDataSendRRDataItem[] List;
        public override ushort GetArraySize(string fieldName)
        {
            if (fieldName == nameof(List))
                return ItemCount;
            throw new Exception(string.Format("Field {0} not found in struct {1} func. {2}", fieldName, nameof(CommandSpecificDataSendRRData), nameof(GetArraySize)));
        }
    }

    public class CommandSpecificDataSendRRDataItem : CIPSerializer
    {
        public CommonPacketItemID TypeID;
        public ushort Length;
    }

    public class CommonIndustrialProtocolRequest : CIPSerializer
    {
        public byte Service;
        public byte RequestPathSize;
        public List<PathSegment> PathSegmentList;
        public override bool IsListCompleted(string fieldName)
        {
            if (fieldName == nameof(PathSegmentList))
            {
                int allItemsTotalBytes = PathSegmentList.Sum(a => ((CIPSerializer)a).SizeOf());
                return allItemsTotalBytes >= (RequestPathSize * 2);
            }
            else
                throw new Exception(string.Format("Field {0} not found in struct {1} func. {2}", fieldName, nameof(CommonIndustrialProtocolRequest), nameof(IsListCompleted)));
        }
    }

    public class CommonIndustrialProtocolReply : CIPSerializer
    {
        public byte Service;
        public byte Reserved;
        public byte GeneralStatus;
        public byte AdditionalStatusSize;
        public ushort[] AdditionalStatus;
        public override ushort GetArraySize(string fieldName)
        {
            if (fieldName == nameof(AdditionalStatus))
                return AdditionalStatusSize;
            throw new Exception(string.Format("Field {0} not found in struct {1} func. {2}", fieldName, nameof(CommonIndustrialProtocolReply), nameof(GetArraySize)));
        }
    }

    public class PathSegment : CIPSerializer
    {
        public static Type GetChildType(byte typeByte)
        {
            SegmentType segType = (SegmentType)(typeByte >> 5);
            switch (segType)
            {
                case SegmentType.LogicalSegment:
                    return typeof(LogicalPathSegment8bits);
                case SegmentType.DataSegment:
                    return typeof(DataPathSegmentANSISymb);
                case SegmentType.PortSegment:
                    return typeof(PortPathSegment);
                default:
                    throw new Exception(String.Format("Segment type not supported yet: {0}", segType));
            }
        }
    }

    public class LogicalPathSegment8bits : PathSegment
    {
        public byte PathSegmentType = 0x20;
        public byte LogicalValue;
        public SegmentLogicalType LogicalType
        {
            get { return (SegmentLogicalType)((PathSegmentType >> 2) & 0x7); }
            set { PathSegmentType = (byte)(((byte)SegmentType.LogicalSegment << 5) | ((byte)value << 2)); }
        }
        public SegmentLogicalFormat LogicalFormat
        {
            get { return (SegmentLogicalFormat)(PathSegmentType & 0x3); }
        }
    }

    public class DataPathSegmentANSISymb : PathSegment
    {
        public byte PathSegmentType = 0x91;
        public byte DataSize;
        public byte[] ANSISymbol;
        public SegmentDataSubType SubType
        {
            get { return (SegmentDataSubType)(PathSegmentType & 0x1F); }
        }
        public override ushort GetArraySize(string fieldName)
        {
            if (fieldName == nameof(ANSISymbol))
                return (ushort)(DataSize % 2 == 0 ? DataSize : DataSize + 1);
            throw new Exception(string.Format("Field {0} not found in struct {1} func. {2}", fieldName, nameof(CommandSpecificDataListServices), nameof(GetArraySize)));
        }
    }

    public class PortPathSegment : PathSegment
    {
        public byte PathSegmentType = 0x1;
        public byte? OptionalLinkAddressSize;
        public ushort? OptionalExtendedPortIdentifier;
        public byte[] LinkAddress;
        public byte? Pad;
        public SegmentDataSubType SubType
        {
            get { return (SegmentDataSubType)(PathSegmentType & 0x1F); }
        }
        public override ushort GetArraySize(string fieldName)
        {
            if (fieldName == nameof(LinkAddress))
                return OptionalLinkAddressSize.HasValue ? OptionalLinkAddressSize.Value : (ushort)0;
            throw new Exception(string.Format("Field {0} not found in struct {1} func. {2}", fieldName, nameof(CommandSpecificDataListServices), nameof(GetArraySize)));
        }
        public override bool HasNullableFieldValue(string fieldName)
        {
            if (fieldName == nameof(OptionalLinkAddressSize))
                return HasOptionalLinkAddressSize;
            else if (fieldName == nameof(OptionalExtendedPortIdentifier))
                return HasOptionalExtendedPortIdentifier;
            else if (fieldName == nameof(Pad))
                return HasPad;
            throw new Exception(string.Format("Field {0} not found in struct {1} func. {2}", fieldName, nameof(CommandSpecificDataListServices), nameof(GetArraySize)));
        }
        public bool HasOptionalLinkAddressSize
        {
            get { return (PathSegmentType & 0x10) != 0; }
        }
        public bool HasOptionalExtendedPortIdentifier
        {
            get { return (PathSegmentType & 0xF) == 15; }
        }
        public bool HasPad
        {
            get { return this.SizeOf() % 2 != 0; }
        }
    }

    public class CIPConnectionManagerUnconnSnd : CIPSerializer
    {
        public byte PriorityAndPickTime;
        public byte TimeOutTicks;
        public ushort MessageRequestSize;
        public CommonIndustrialProtocolRequest CommonIndustrialProtocolRequest;
        public CIPClassGeneric CIPClassGeneric;
        public byte? Pad;
        public byte RoutePathSize;
        public byte Reserved;
        public List<PathSegment> RoutePath;
        public override bool IsListCompleted(string fieldName)
        {
            if (fieldName == nameof(RoutePath))
            {
                int allItemsTotalBytes = RoutePath.Sum(a => ((CIPSerializer)a).SizeOf());
                return allItemsTotalBytes >= (RoutePathSize * 2);
            }
            else
                throw new Exception(string.Format("Field {0} not found in struct {1} func. {2}", fieldName, nameof(CommonIndustrialProtocolRequest), nameof(IsListCompleted)));
        }
        public override bool HasNullableFieldValue(string fieldName)
        {
            if (fieldName == nameof(Pad))
            {
                int byteCount = CommonIndustrialProtocolRequest.SizeOf() +
                                CIPClassGeneric.SizeOf() +
                                Marshal.SizeOf(RoutePathSize);
                return byteCount % 2 != 0;
            }
            throw new Exception(string.Format("Field {0} not found in struct {1} func. {2}", fieldName, nameof(CommandSpecificDataListServices), nameof(HasNullableFieldValue)));
        }
    }

    public class CIPClassGeneric : CIPSerializer
    {
        public ElementaryDataType DataType;
        public ushort SpecificDataSize;
        public byte[] CIPClassGenericCmdSpecificData;
        public override ushort GetArraySize(string fieldName)
        {
            if (fieldName == nameof(CIPClassGenericCmdSpecificData))
                return (ushort)(SpecificDataSize * GetDataTypeSize(DataType));
            throw new Exception(string.Format("Field {0} not found in struct {1} func. {2}", fieldName, nameof(CIPClassGeneric), nameof(GetArraySize)));
        }
        public static byte GetDataTypeSize(ElementaryDataType dataType)
        {
            switch (dataType)
            {
                case ElementaryDataType.SINT: return 1;
                case ElementaryDataType.INT: return 2;
                case ElementaryDataType.DINT: return 4;
                case ElementaryDataType.LINT: return 8;
                case ElementaryDataType.USINT: return 1;
                case ElementaryDataType.UINT: return 2;
                case ElementaryDataType.UDINT: return 4;
                case ElementaryDataType.ULINT: return 8;
                case ElementaryDataType.REAL: return 4;
                case ElementaryDataType.STRING: return 1;
                case ElementaryDataType.BYTE: return 1;
                default:
                    throw new Exception(string.Format("Data type '{0}' not implemented yet", dataType));
            }
        }
    }

    public class MsgListServiceReply : CIPSerializer
    {
        public CommandSpecificDataListServices CommandSpecificDataListServices;
    }

    public class MsgRegisterSessionRequest : CIPSerializer
    {
        public CommandSpecificDataRegisterSession CommandSpecificDataRegisterSession;
    }

    public class MsgRegisterSessionReply : CIPSerializer
    {
        public CommandSpecificDataRegisterSession CommandSpecificDataRegisterSession;
    }

    public class MsgUnconnectedSendRequest : CIPSerializer
    {
        public CommandSpecificDataSendRRData CommandSpecificDataSendRRData;
        public CommonIndustrialProtocolRequest CommonIndustrialProtocolRequest;
        public CIPConnectionManagerUnconnSnd CIPConnectionManagerUnconnSnd;
    }

    public class MsgUnconnectedSendReply : CIPSerializer
    {
        public CommandSpecificDataSendRRData CommandSpecificDataSendRRData;
        public CommonIndustrialProtocolReply CommonIndustrialProtocolReply;
    }    
}