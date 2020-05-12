// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Structs
// -----------------------------------------------------------------------------------------------------------------
using System;

namespace Techsteel.Drivers.CIP
{
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
}
