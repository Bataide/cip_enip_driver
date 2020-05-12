// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Enums
// -----------------------------------------------------------------------------------------------------------------
using System;

namespace Techsteel.Drivers.CIP
{
    public enum SegmentType
    {
        PortSegment = 0,
        LogicalSegment = 1,
        NetworkSegment = 2,
        SymbolicSegment = 3,
        DataSegment = 4,
        DataTypeConstructed = 5,
        DataTypeElementary = 6,
        ReservedFuture = 7,
    }
}
