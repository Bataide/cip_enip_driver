// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Enums
// -----------------------------------------------------------------------------------------------------------------
using System;

namespace Techsteel.Drivers.CIP
{
    public enum SegmentLogicalType
    {
        ClassID = 0,
        InstanceID = 1,
        MemberID = 2,
        ConnectionPoint = 3,
        AttributeID = 4,
        Special = 5,
        ServiceID = 6,
        ReservedFuture = 7,
    }
}
