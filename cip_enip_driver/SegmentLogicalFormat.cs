// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Enums
// -----------------------------------------------------------------------------------------------------------------
using System;

namespace Techsteel.Drivers.CIP
{
    public enum SegmentLogicalFormat
    {
        slf_8_bits = 0,
        slf_16_bits = 1,
        slf_32_bits = 2,
        slf_Reserved = 3,
    }
}
