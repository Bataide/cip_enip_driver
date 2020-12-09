// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Structs
// -----------------------------------------------------------------------------------------------------------------
using System;

namespace Techsteel.Drivers.CIP
{
    public class CommandSpecificDataSendRRData : CIPSerializer
    {
        public uint InterfaceHandle;
        public ushort Timeout;
        public ushort ItemCount;
        public CommandSpecificDataSendRRDataItem[] List;
        public override ushort GetArraySize(string fieldName)
        {
            if (fieldName == nameof(List))
                return ItemCount;
            throw new Exception(string.Format("Field {0} not found in struct {1} func. {2}", fieldName, nameof(CommandSpecificDataSendRRData), nameof(GetArraySize)));
        }
    }
}
