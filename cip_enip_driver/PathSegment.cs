// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Structs
// -----------------------------------------------------------------------------------------------------------------
using System;

namespace Techsteel.Drivers.CIP
{
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
}
