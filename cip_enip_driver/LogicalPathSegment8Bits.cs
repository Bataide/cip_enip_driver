// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Structs
// -----------------------------------------------------------------------------------------------------------------
using System;

namespace Techsteel.Drivers.CIP
{
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
}
