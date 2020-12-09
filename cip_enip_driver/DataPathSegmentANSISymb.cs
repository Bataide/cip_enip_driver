// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Structs
// -----------------------------------------------------------------------------------------------------------------
using System;

namespace Techsteel.Drivers.CIP
{
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
}
