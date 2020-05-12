// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Enums
// -----------------------------------------------------------------------------------------------------------------
using System;

namespace Techsteel.Drivers.CIP
{
    public enum CipStatusCode
    {
        Success = 0x00,
        ConnectionFailure = 0x01,
        ResourceUnavailable = 0x02,
        InvalidParameterValue = 0x03,
        PathSegmentError = 0x04,
        PathDestinationUnknown = 0x05,
        PartialTransfer = 0x06,
        ConnectionLost = 0x07,
        ServiceNotSupported = 0x08,
        InvalidAttributeValue = 0x09,
        AttributeListError = 0x0A,
        AlreadyInRequestedModeState = 0x0B,
        ObjectStateConflict = 0x0C,
        ObjectAlreadyExists = 0x0D,
        AttributeNotSettable = 0x0E,
        PrivilegeViolation = 0x0F,
        DeviceStateConflict = 0x10,
        ReplyDataTooLarge = 0x11,
        FragmentationOfAPrimitiveValue = 0x12,
        NotEnoughData = 0x13,
        AttributeNotSupported = 0x14,
        TooMuchData = 0x15,
        ObjectDoesNotExist = 0x16,
        ServiceFragmentationSequenceNotInProgress = 0x17,
        NoStoredAttributeData = 0x18,
        StoreOperationFailure = 0x19,
        RoutingFailureRequestPacketTooLarge = 0x1A,
        RoutingFailureResponsePacketTooLarge = 0x1B,
        MissingAttributeListEntryData = 0x1C,
        InvalidAttributeValueList = 0x1D,
        EmbeddedServiceError = 0x1E,
        VendorSpecificError = 0x1F,
        InvalidParameter = 0x20,
        WriteOnceValueOrMediumAlreadyWritten = 0x21,
        InvalidReplyReceived = 0x22,
        DescriptionOfStatusReservedByCIPForFutureExtensions1 = 0x23,
        DescriptionOfStatusReservedByCIPForFutureExtensions2 = 0x24,
        KeyFailureInPath = 0x25,
        PathSizeInvalid = 0x26,
        UnexpectedAttributeInList = 0x27,
        InvalidMemberID = 0x28,
        MemberNotSettable = 0x29,
        Group2OnlyServerGeneral = 0x2A,
    }
}
