// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Class: CIP
// Description: Class representing an incoming connection
// -----------------------------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace Techsteel.Drivers.CIP
{    
    public class CIPConn : Traceable
    {
        public event CIP.DlgConnRecReceivedMsgData OnReceiveData;

        private string LOG_TAG = "CIP RCV CONN.";
        private MemoryStream m_ReceiveBuffer = new MemoryStream();
        private SocketConn m_ScktConn;
        private DateTime m_WaitingRemainingBytes = DateTime.MinValue;
        private Thread m_Thread;
        private bool m_Terminate;
        private AutoResetEvent m_ThreadResetEvent = new AutoResetEvent(false);
        private List<byte[]> m_SendMsgList = new List<byte[]>();
        private uint m_SessionHandle = 0;
        private DateTime m_ActivityTimeRef = DateTime.MinValue;

        public CIPConn(SocketConn scktConn)
        {
            LOG_TAG += string.Format(" ({0})", scktConn.RemoteEndPoint);
            m_ActivityTimeRef = DateTime.Now;
            m_ScktConn = scktConn;
            m_ScktConn.OnDisconnect += ScktConn_OnDisconnect;
            m_ScktConn.OnReceiveData += ScktConn_OnReceiveData;
            m_ScktConn.OnReceiveError += ScktConn_OnReceiveError;
            m_ScktConn.OnSendError += ScktConn_OnSendError;
            Trace(EventType.Info, string.Format("{0} - Connection established: {1}", LOG_TAG, m_ScktConn.RemoteEndPoint));
        }

        public void Open()
        {
            if (m_Thread == null)
            {
                m_Thread = new Thread(ThreadTask);
                m_Thread.Start();            
            }
        }

        public void Close()
        {
            Trace(EventType.Info, string.Format("{0} - CIP conn. will be closed", LOG_TAG));
            m_ScktConn.Close();
            m_Terminate = true;
            m_ThreadResetEvent.Set();
        }

        private void ScktConn_OnDisconnect(SocketConn scktConn)
        {           
            try
            {
                Trace(EventType.Info, string.Format("{0} - Connection is closed: {1}", LOG_TAG, scktConn.RemoteEndPoint));
                Close();                
            }
            catch (Exception e)
            {
                Trace(e);
            }
        }

        private void ScktConn_OnReceiveData(SocketConn scktConn, byte[] data)
        {
            try
            {
                ReceiveBytes(data);
            }
            catch (Exception e)
            {
                Trace(e);
            }
        }

        private void ScktConn_OnReceiveError(SocketConn scktConn, Exception scktExp)
        {            
            Trace(EventType.Error, string.Format("{0} - Receive socket error {1}", LOG_TAG, scktExp.Message));
        }

        private void ScktConn_OnSendError(SocketConn scktConn, Exception scktExp)
        {            
            Trace(EventType.Error, string.Format("{0} - Send socket error {1}", LOG_TAG, scktExp.Message));
        }        

        public void ReceiveBytes(byte[] data)
        {
            m_ActivityTimeRef = DateTime.Now;
            Trace(EventType.Info, string.Format("{0} - {1} bytes received!", LOG_TAG, data.Length));
            m_ReceiveBuffer.Position = m_ReceiveBuffer.Length;
            m_ReceiveBuffer.Write(data, 0, data.Length);
            long pointer = 0;
            while (pointer < m_ReceiveBuffer.Length)
            {
                long msgHeaderSize = Marshal.SizeOf(typeof(CommandEtherNetIPHeader));
                if (m_ReceiveBuffer.Length - pointer >= msgHeaderSize)
                {
                    byte[] bytes = new byte[msgHeaderSize];
                    m_ReceiveBuffer.Position = pointer;
                    m_ReceiveBuffer.Read(bytes, 0, bytes.Length);
                    CommandEtherNetIPHeader headerStructObj = CommandEtherNetIPHeader.Deserialize(bytes);
                    long msgSize = msgHeaderSize + headerStructObj.Length;
                    if (m_ReceiveBuffer.Length - pointer >= msgSize)
                    {
                        byte[] bodyBytes = new byte[headerStructObj.Length];
                        m_ReceiveBuffer.Position = pointer + msgHeaderSize;
                        m_ReceiveBuffer.Read(bodyBytes, 0, bodyBytes.Length);
                        MessageFactory(headerStructObj, bodyBytes);
                        m_WaitingRemainingBytes = DateTime.MinValue;
                        pointer += msgSize;
                    }
                    else
                    {
                        Trace(EventType.Warning, string.Format("{0} - Waiting for the rest of msg. body", LOG_TAG));
                        m_WaitingRemainingBytes = DateTime.Now;
                        break;
                    }
                }
                else
                {
                    Trace(EventType.Warning, string.Format("{0} - Waiting for the rest of msg. header", LOG_TAG));
                    m_WaitingRemainingBytes = DateTime.Now;
                    break;
                }
            }
            if (pointer >= m_ReceiveBuffer.Length)
            {                
                m_ReceiveBuffer.SetLength(0);
                m_ReceiveBuffer.Capacity = 0;
                m_ReceiveBuffer.Position = 0;                
                Trace(EventType.Info, string.Format("{0} - Receive buffer clear!", LOG_TAG));
            }
        }
        
        private void MessageFactory(CommandEtherNetIPHeader header, byte[] bodyBytes)
        {
            try
            {
                Trace(EventType.Info, string.Format("{0} - Receive msg. '{1}'", LOG_TAG, header.Command));
                long headerSize = Marshal.SizeOf(typeof(CommandEtherNetIPHeader));            
                switch (header.Command)
                {
                    case EncapsulationCommands.ListServices:
                    {                  
                        if (bodyBytes.Length == 0)
                        {
                            MsgListServiceReply msg = new MsgListServiceReply();                        
                            msg.CommandSpecificDataListServices = new CommandSpecificDataListServices();
                            msg.CommandSpecificDataListServices.ItemCount = 1;
                            msg.CommandSpecificDataListServices.Items = new CommandSpecificDataListServicesItem[1];
                            msg.CommandSpecificDataListServices.Items[0] = new CommandSpecificDataListServicesItem
                                {
                                    TypeCode = CommonPacketItemID.ListServicesResponse,
                                    Version = 1,
                                    CapabilityFlags = Convert.ToUInt16("100100000", 2),
                                    ServiceName = Encoding.ASCII.GetBytes("Communications\0\0")
                                };
                            header.Length = msg.SizeOf();
                            msg.CommandSpecificDataListServices.Items[0].Length =
                                (ushort)(msg.CommandSpecificDataListServices.Items[0].SizeOf() - 2 - 2);
                            SendMessage(header, msg);
                        }
                        break;
                    }

                    case EncapsulationCommands.RegisterSession:
                    {
                        int pointer = 0;
                        CommandSpecificDataRegisterSession cmdSpecData =
                            (CommandSpecificDataRegisterSession)CommandSpecificDataListServices.Deserialize(
                                typeof(CommandSpecificDataRegisterSession), bodyBytes, ref pointer);
                        MsgRegisterSessionReply msg = new MsgRegisterSessionReply();
                        // TODO: check the protocol version to accept the registration
                        msg.CommandSpecificDataRegisterSession = cmdSpecData;
                        m_SessionHandle = (uint)((DateTime.Now.Ticks / 10) & 0xFFFFFFFF);
                        header.SessionHandle = m_SessionHandle;
                        header.Length = msg.SizeOf();
                        SendMessage(header, msg);
                        break;
                    }

                    case EncapsulationCommands.SendRRData:
                    {
                        if (header.SessionHandle != m_SessionHandle)
                            throw new Exception(string.Format("Received invalid session handle (unregistred) in SendRRData message: ", header.SessionHandle));
                        int pointer = 0;
                        MsgUnconnectedSendRequest unconnSndReq = (MsgUnconnectedSendRequest)MsgUnconnectedSendRequest.Deserialize(
                            typeof(MsgUnconnectedSendRequest), bodyBytes, ref pointer);
                        string symbol = Encoding.ASCII.GetString(((DataPathSegmentANSISymb)unconnSndReq.CIPConnectionManagerUnconnSnd.CommonIndustrialProtocolRequest.PathSegmentList[0]).ANSISymbol);
                        ElementaryDataType dataType = unconnSndReq.CIPConnectionManagerUnconnSnd.CIPClassGeneric.DataType;
                        OnReceiveData?.Invoke(m_ScktConn.RemoteEndPoint, symbol, dataType, unconnSndReq.CIPConnectionManagerUnconnSnd.CIPClassGeneric.CIPClassGenericCmdSpecificData);
                        MsgUnconnectedSendReply msg = new MsgUnconnectedSendReply();
                        msg.CommandSpecificDataSendRRData = unconnSndReq.CommandSpecificDataSendRRData;
                        CommandSpecificDataSendRRDataItem item = msg.CommandSpecificDataSendRRData.List.First(
                            a => a.TypeID == CommonPacketItemID.UnconnectedMessage);                        
                        msg.CommonIndustrialProtocolReply = new CommonIndustrialProtocolReply {
                            Service = 0xcd,
                            Reserved = 0x0,
                            GeneralStatus = 0x0, // <- success
                            AdditionalStatusSize = 0x0,
                            AdditionalStatus = new ushort[0]
                        };
                        item.Length = msg.CommonIndustrialProtocolReply.SizeOf();
                        header.Length = msg.SizeOf();
                        SendMessage(header, msg);
                        break;
                    }

                    case EncapsulationCommands.UnRegisterSession:
                    {                    
                        break;
                    }

                    default:
                    {
                        Trace(EventType.Error, string.Format("{0} - Command {1} not implemented", LOG_TAG, header.Command));
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Trace(e);
            }
        }

        private void ThreadTask()
        {
            try
            {
                while (!m_Terminate)
                {
                    if (m_WaitingRemainingBytes != DateTime.MinValue)
                        if (DateTime.Now.Subtract(m_WaitingRemainingBytes).TotalSeconds > 2)
                        {
                            Trace(EventType.Error, string.Format("{0} - So long time waiting the rest of message bytes. The connection will be closed. {1}", LOG_TAG, m_ScktConn.RemoteEndPoint));
                            Close();
                        }

                    lock (m_SendMsgList)
                        if (m_SendMsgList.Count > 0)
                        {
                            byte[] msgBytes = m_SendMsgList[0];
                            m_ScktConn.SendData(msgBytes);
                            Trace(EventType.Info, string.Format("{0} - {1} bytes sent !!!", LOG_TAG, msgBytes.Length));
                            m_SendMsgList.RemoveAt(0);
                        }

                    if (!m_Terminate)
                        if (!m_ThreadResetEvent.WaitOne(2000) && m_SendMsgList.Count == 0)
                        {
                            CommandEtherNetIPHeader msgListServices = new CommandEtherNetIPHeader { Command = EncapsulationCommands.NOP };
                            SendMessage(msgListServices, null);
                        }
                }
            }
            catch (Exception exc)
            {
                Trace(exc);
            }
        }    

        private void SendMessage(CommandEtherNetIPHeader header, CIPSerializer msg)
        {
            byte[] headerBytes = header.Serialize();
            byte[] msgBytes = msg?.Serialize() ?? new byte[0];
            byte[] allBytes = new byte[headerBytes.Length + msgBytes.Length];
            Array.Copy(headerBytes, allBytes, headerBytes.Length);
            Array.Copy(msgBytes, 0, allBytes, headerBytes.Length, msgBytes.Length);
            Trace(EventType.Info, string.Format("{0} - Msg. '{1}' queued to be send", LOG_TAG, header.Command));
            lock (m_SendMsgList)
                m_SendMsgList.Add(allBytes);
            m_ThreadResetEvent.Set();            
        }
    }
}