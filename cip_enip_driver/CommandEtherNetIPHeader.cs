// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Description: Structs
// -----------------------------------------------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;

namespace Techsteel.Drivers.CIP
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class CommandEtherNetIPHeader
    {
        public const int SENDER_CONTEXT_SIZE = 8;
        public EncapsulationCommands Command;
        public ushort Length;
        public uint SessionHandle;
        public uint Status;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SENDER_CONTEXT_SIZE)]
        public byte[] SenderContext = new byte[SENDER_CONTEXT_SIZE];
        public uint Options;
        public static CommandEtherNetIPHeader Deserialize(byte[] buffer)
        {
            object obj = null;
            Type objType = typeof(CommandEtherNetIPHeader);
            if ((buffer != null) && (buffer.Length > 0))
            {
                IntPtr ptrObj = IntPtr.Zero;
                try
                {
                    int objSize = Marshal.SizeOf(objType);
                    if (objSize > 0)
                    {
                        if (buffer.Length < objSize)
                            throw new Exception(string.Format("Insufficient byte amount to deserialize 'Encapsulation Header' struct ({0} byte(s)", buffer.Length));
                        ptrObj = Marshal.AllocHGlobal(objSize);
                        if (ptrObj != IntPtr.Zero)
                        {
                            Marshal.Copy(buffer, 0, ptrObj, objSize);
                            obj = Marshal.PtrToStructure(ptrObj, objType);
                        }
                        else
                            throw new Exception("Could not allocate memory to deserialize 'Encapsulation Header' struct");
                    }
                }
                finally
                {
                    if (ptrObj != IntPtr.Zero)
                        Marshal.FreeHGlobal(ptrObj);
                }
            }
            return (CommandEtherNetIPHeader)obj;
        }
        public byte[] Serialize()
        {
            byte[] buffer = null;
            if (this != null)
            {
                Type objType = this.GetType();
                int objSize = Marshal.SizeOf(objType);
                if (objSize > 0)
                {
                    IntPtr ptrObj = IntPtr.Zero;
                    try
                    {
                        buffer = new byte[objSize];
                        ptrObj = Marshal.AllocHGlobal(objSize);
                        if (ptrObj != IntPtr.Zero)
                        {
                            Marshal.StructureToPtr(this, ptrObj, true);
                            Marshal.Copy(ptrObj, buffer, 0, buffer.Length);
                        }
                        else
                            throw new Exception("Fail to allocate memory to receive deserialization bytes");
                    }
                    finally
                    {
                        if (ptrObj != IntPtr.Zero)
                            Marshal.FreeHGlobal(ptrObj);
                    }
                    if (buffer == null)
                        throw new Exception("General failure on byte array creation during serialization");
                }
            }
            return buffer;
        }
    }
}
