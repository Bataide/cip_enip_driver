// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Enums
// -----------------------------------------------------------------------------------------------------------------
using System;

namespace Techsteel.Drivers.CIP
{
    public enum CommonPacketItemID : ushort
    {
        Null = 0x0000,
        ListIdentity = 0x000C,
        ConnectionBased = 0x00A1,
        ConnectedTransportPacket = 0x00B1,
        UnconnectedMessage = 0x00B2,
        ListServicesResponse = 0x0100,
        SockaddrInfoOtoT = 0x8000,
        SockaddrInfoTtoO = 0x8001,
        SequencedAddressItem = 0x8002,
    }
}
