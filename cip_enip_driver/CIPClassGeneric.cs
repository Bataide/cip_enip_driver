// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Structs
// -----------------------------------------------------------------------------------------------------------------
using System;

namespace Techsteel.Drivers.CIP
{
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
}
