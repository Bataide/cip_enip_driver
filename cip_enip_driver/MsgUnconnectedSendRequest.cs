// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Structs
// -----------------------------------------------------------------------------------------------------------------
using System;

namespace Techsteel.Drivers.CIP
{
    public class MsgUnconnectedSendRequest : CIPSerializer
    {
        public CommandSpecificDataSendRRData CommandSpecificDataSendRRData;
        public CommonIndustrialProtocolRequest CommonIndustrialProtocolRequest;
        public CIPConnectionManagerUnconnSnd CIPConnectionManagerUnconnSnd;
    }
}
