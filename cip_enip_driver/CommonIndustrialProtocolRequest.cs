// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Structs
// -----------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;

namespace Techsteel.Drivers.CIP
{
    public class CommonIndustrialProtocolRequest : CIPSerializer
    {
        public byte Service;
        public byte RequestPathSize;
        public List<PathSegment> PathSegmentList;
        public override bool IsListCompleted(string fieldName)
        {
            if (fieldName == nameof(PathSegmentList))
            {
                int allItemsTotalBytes = PathSegmentList.Sum(a => ((CIPSerializer)a).SizeOf());
                return allItemsTotalBytes >= (RequestPathSize * 2);
            }
            else
                throw new Exception(string.Format("Field {0} not found in struct {1} func. {2}", fieldName, nameof(CommonIndustrialProtocolRequest), nameof(IsListCompleted)));
        }
    }
}
