// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Enums
// -----------------------------------------------------------------------------------------------------------------
using System;

namespace Techsteel.Drivers.CIP
{
    public enum ElementaryDataType : ushort
    {
        BOOL = 0xC1,
        SINT = 0xC2,
        INT = 0xC3,
        DINT = 0xC4,
        LINT = 0xC5,
        USINT = 0xC6,
        UINT = 0xC7,
        UDINT = 0xC8,
        ULINT = 0xC9,
        REAL = 0xCA,
        LREAL = 0xCB,
        STIME = 0xCC,
        DATE = 0xCD,
        TIME_OF_DAY = 0xCE,
        DATE_AND_TIME = 0xCF,
        STRING = 0xD0,
        BYTE = 0xD1,
        WORD = 0xD2,
        DWORD = 0xD3,
        LWORD = 0xD4,
        STRING2 = 0xD5,
        FTIME = 0xD6,
        LTIME = 0xD7,
        ITIME = 0xD8,
        STRINGN = 0xD9,
        SHORT_STRING = 0xDA,
        TIME = 0xDB,
        EPATH = 0xDC,
        ENGUNIT = 0xDD,
    }
}
