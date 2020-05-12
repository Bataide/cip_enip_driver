// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Enums
// -----------------------------------------------------------------------------------------------------------------
using System;

namespace Techsteel.Drivers.CIP
{
    public enum ConnMngrObjectInstSpecificServicesCode
    {
        Forward_Close = 0x4E,
        Unconnected_Send = 0x52,
        Forward_Open = 0x54,
        Get_Connection_Data = 0x56,
        Search_Connection_Data = 0x57,
        Ex_Forward_Open = 0x59,
        Get_Connection_Owner = 0x5A,
    }
}
