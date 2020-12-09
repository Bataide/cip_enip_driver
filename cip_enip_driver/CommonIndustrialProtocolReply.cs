// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Structs
// -----------------------------------------------------------------------------------------------------------------
using System;

namespace Techsteel.Drivers.CIP
{
    public class CommonIndustrialProtocolReply : CIPSerializer
    {
        public byte Service;
        public byte Reserved;
        public byte GeneralStatus;
        public byte AdditionalStatusSize;
        public ushort[] AdditionalStatus;
        public override ushort GetArraySize(string fieldName)
        {
            if (fieldName == nameof(AdditionalStatus))
                return AdditionalStatusSize;
            throw new Exception(string.Format("Field {0} not found in struct {1} func. {2}", fieldName, nameof(CommonIndustrialProtocolReply), nameof(GetArraySize)));
        }
    }
}
