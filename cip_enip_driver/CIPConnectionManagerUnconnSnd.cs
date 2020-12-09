// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Structs
// -----------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Techsteel.Drivers.CIP
{
    public class CIPConnectionManagerUnconnSnd : CIPSerializer
    {
        public byte PriorityAndPickTime;
        public byte TimeOutTicks;
        public ushort MessageRequestSize;
        public CommonIndustrialProtocolRequest CommonIndustrialProtocolRequest;
        public CIPClassGeneric CIPClassGeneric;
        public byte? Pad;
        public byte RoutePathSize;
        public byte Reserved;
        public List<PathSegment> RoutePath;
        public override bool IsListCompleted(string fieldName)
        {
            if (fieldName == nameof(RoutePath))
            {
                int allItemsTotalBytes = RoutePath.Sum(a => ((CIPSerializer)a).SizeOf());
                return allItemsTotalBytes >= (RoutePathSize * 2);
            }
            else
                throw new Exception(string.Format("Field {0} not found in struct {1} func. {2}", fieldName, nameof(CommonIndustrialProtocolRequest), nameof(IsListCompleted)));
        }
        public override bool HasNullableFieldValue(string fieldName)
        {
            if (fieldName == nameof(Pad))
            {
                int byteCount = CommonIndustrialProtocolRequest.SizeOf() +
                                CIPClassGeneric.SizeOf() +
                                Marshal.SizeOf(RoutePathSize);
                return byteCount % 2 != 0;
            }
            throw new Exception(string.Format("Field {0} not found in struct {1} func. {2}", fieldName, nameof(CommandSpecificDataListServices), nameof(HasNullableFieldValue)));
        }
    }
}
