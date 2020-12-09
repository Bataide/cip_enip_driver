// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Structs
// -----------------------------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq;

namespace Techsteel.Drivers.CIP
{
    public class PortPathSegment : PathSegment
    {
        public byte PathSegmentType = 0x1;
        public byte? OptionalLinkAddressSize;
        public ushort? OptionalExtendedPortIdentifier;
        public byte[] LinkAddress;
        public byte? Pad;
        public SegmentDataSubType SubType
        {
            get { return (SegmentDataSubType)(PathSegmentType & 0x1F); }
        }
        public override ushort GetArraySize(string fieldName)
        {
            if (fieldName == nameof(LinkAddress))
                return OptionalLinkAddressSize.HasValue ? OptionalLinkAddressSize.Value : (ushort)0;
            throw new Exception(string.Format("Field {0} not found in struct {1} func. {2}", fieldName, nameof(CommandSpecificDataListServices), nameof(GetArraySize)));
        }
        public override bool HasNullableFieldValue(string fieldName)
        {
            if (fieldName == nameof(OptionalLinkAddressSize))
                return HasOptionalLinkAddressSize;
            else if (fieldName == nameof(OptionalExtendedPortIdentifier))
                return HasOptionalExtendedPortIdentifier;
            else if (fieldName == nameof(Pad))
                return HasPad;
            throw new Exception(string.Format("Field {0} not found in struct {1} func. {2}", fieldName, nameof(CommandSpecificDataListServices), nameof(GetArraySize)));
        }
        public bool HasOptionalLinkAddressSize
        {
            get { return (PathSegmentType & 0x10) != 0; }
        }
        public bool HasOptionalExtendedPortIdentifier
        {
            get { return (PathSegmentType & 0xF) == 15; }
        }
        public bool HasPad
        {
            get { return this.SizeOf() % 2 != 0; }
        }
    }
}
