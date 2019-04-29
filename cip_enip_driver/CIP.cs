// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Class: CIP
// Description: Class responsible to establish outcoming connection (only one) and receive incoming
// connections (multiples)
// -----------------------------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Techsteel.Drivers.CIP
{    
    public class CIP : Traceable
    {
        public enum ConnType 
        {
            Send,
            Receive
        }

        private enum ClientConnStates
        {
            Disconnected,
            SendListServices,
            WaitListServicesReply,
            SendRegisterSession,
            WaitRegisterSessionReply,
            SendReceive,
        }

        private const int DEFAULT_PORT = 0xAF12;
        private const string LOG_TAG = "CIP SND CONN.";
        private const int SEND_TIME_OUT = 2000;

        public delegate void DlgConnStatusChanged(ConnType connType, bool connected, string connID);
        public delegate void DlgConnRecReceivedMsgData(string remoteEndPoint, string symbol, ElementaryDataType dataType, byte[] data);
        
        public event DlgConnStatusChanged OnConnStatusChanged;
        public event DlgConnRecReceivedMsgData OnConnRecReceiveMsgData;
        
        private SocketServer m_SocketServer;
        private SocketClient m_SocketClient;
        private Dictionary<SocketConn, CIPConn> m_CIPConnList = new Dictionary<SocketConn, CIPConn>();
        private Thread m_Thread;
        private bool m_Terminate;
        private AutoResetEvent m_ThreadResetEvent = new AutoResetEvent(false);
        private ClientConnStates m_ClientConnStates = ClientConnStates.Disconnected;
        private DateTime m_ActivityTimeRef = DateTime.MinValue;
        private MemoryStream m_ReceiveBuffer = new MemoryStream();
        private DateTime m_WaitingRemainingBytes = DateTime.MinValue;
        private uint m_SessionHandle = 0;
        private object m_SndMutex = new object();
        private long m_SenderContext;
        private byte[] m_DataToSend;
        private ElementaryDataType m_DataTypeToSend;
        private string m_Symbol;
        private byte? m_SendStatusResult;
        private ManualResetEvent m_SendResetEvent = new ManualResetEvent(false);        
        
        public CIP(string localAddress, string remoteAddress) : this (localAddress, DEFAULT_PORT, remoteAddress, DEFAULT_PORT)
        {
        }

        public CIP(string localAddress, int localPort, string remoteAddress, int remotePort) : base()
        {
            m_SocketServer = new SocketServer(localAddress, localPort);
            m_SocketServer.OnEventTrace += SocketServer_OnEventTrace;
            m_SocketServer.OnConnect += SocketServer_OnConnect;
            m_SocketServer.OnDisconnect += SocketServer_OnDisconnect;

            m_SocketClient = new SocketClient(remoteAddress, remotePort);
            m_SocketClient.OnEventTrace += SocketClient_OnEventTrace;
            m_SocketClient.OnConnect += SocketClient_OnConnect;
            m_SocketClient.OnDisconnect += SocketClient_OnDisconnect;
            m_SocketClient.OnConnectError += SocketClient_OnConnectError;
            m_SocketClient.OnReceiveData += SocketClient_OnReceiveData;
            m_SocketClient.OnReceiveError += SocketClient_OnReceiveError;
            m_SocketClient.OnSendError += SocketClient_OnSendError;
        }

        public void Open()
        {
            if (m_Thread == null)
            {
                m_Thread = new Thread(ThreadTask);
                m_Thread.Start();
            }

            m_SocketServer.Open();
            m_SocketClient.Open();
        }

        public void Close()
        {
            m_SocketServer.TerminateListen();
            m_SocketServer.CloseAllConnections();
            m_Terminate = true;
            m_SocketClient.Close();
            m_ThreadResetEvent.Set();
        }

        public bool SendChannelConnected()
        {
            return m_SocketClient?.Connected ?? false;
        }

        public bool ReadyToSend()
        {
            return m_ClientConnStates == ClientConnStates.SendReceive;
        }

        public int RecConnCount()
        {
            return m_CIPConnList?.Count() ?? 0;
        }

        private void ThreadTask()
        {
            try
            {
                while (!m_Terminate)
                {
                    switch (m_ClientConnStates)
                    {
                        case ClientConnStates.Disconnected:
                        {
                            break;
                        }
                        case ClientConnStates.SendListServices:
                        {
                            CommandEtherNetIPHeader msgListServices = new CommandEtherNetIPHeader { Command = EncapsulationCommands.ListServices };
                            SendMessage(msgListServices, null);
                            m_ClientConnStates = ClientConnStates.WaitListServicesReply;
                            break;
                        }
                        case ClientConnStates.SendRegisterSession:
                        {
                            MsgRegisterSessionRequest msg = new MsgRegisterSessionRequest();
                            CommandEtherNetIPHeader header = new CommandEtherNetIPHeader { Command = EncapsulationCommands.RegisterSession };
                            msg.CommandSpecificDataRegisterSession = new CommandSpecificDataRegisterSession { ProtocolVersion = 1 };
                            header.Length = msg.CommandSpecificDataRegisterSession.SizeOf();
                            SendMessage(header, msg);
                            m_ClientConnStates = ClientConnStates.WaitRegisterSessionReply;
                            break;
                        }
                        case ClientConnStates.SendReceive:
                        {
                            if (m_DataToSend != null)
                            {
                                CommandEtherNetIPHeader header = new CommandEtherNetIPHeader {
                                    Command = EncapsulationCommands.SendRRData,
                                    SessionHandle = m_SessionHandle,
                                    SenderContext = BitConverter.GetBytes(m_SenderContext)
                                };
                                MsgUnconnectedSendRequest msg = new MsgUnconnectedSendRequest {
                                    CommandSpecificDataSendRRData = new CommandSpecificDataSendRRData {
                                        InterfaceHandle = 0,
                                        Timeout = 0,                                        
                                        ItemCount = 2,
                                        List = new CommandSpecificDataSendRRDataItem[] {
                                            new CommandSpecificDataSendRRDataItem {
                                                TypeID = 0,
                                                Length = 0
                                            },
                                            new CommandSpecificDataSendRRDataItem {
                                                TypeID = CommonPacketItemID.UnconnectedMessage,
                                                Length = 0
                                            }
                                        }
                                    },
                                    CommonIndustrialProtocolRequest = new CommonIndustrialProtocolRequest {
                                        Service = 0x52,
                                        RequestPathSize = 2,
                                        PathSegmentList = new List<PathSegment> {
                                            new LogicalPathSegment8bits {
                                                PathSegmentType = 0x20,
                                                LogicalValue = 0x06
                                            },
                                            new LogicalPathSegment8bits {
                                                PathSegmentType = 0x24,
                                                LogicalValue = 0x01
                                            }
                                        }
                                    },
                                    CIPConnectionManagerUnconnSnd = new CIPConnectionManagerUnconnSnd {
                                        PriorityAndPickTime = 0x07,                                        
                                        TimeOutTicks = 233,
                                        MessageRequestSize = 0,
                                        CommonIndustrialProtocolRequest = new CommonIndustrialProtocolRequest {
                                            Service = 0x4d,
                                            RequestPathSize = 0,
                                            PathSegmentList = new List<PathSegment> {
                                                new DataPathSegmentANSISymb {
                                                    PathSegmentType = 0x91,
                                                    DataSize = (byte)m_Symbol.Length,
                                                    ANSISymbol = Encoding.ASCII.GetBytes(m_Symbol.Length % 2 == 0 ? m_Symbol : m_Symbol + "\0")
                                                }
                                            }
                                        },
                                        CIPClassGeneric = new CIPClassGeneric {
                                            DataType = m_DataTypeToSend,
                                            SpecificDataSize = (ushort)(m_DataToSend.Length / CIPClassGeneric.GetDataTypeSize(m_DataTypeToSend)),
                                            CIPClassGenericCmdSpecificData = m_DataToSend,
                                        },
                                        Pad = null,
                                        RoutePathSize = 0,
                                        Reserved = 0,
                                        RoutePath = new List<PathSegment> {
                                            new PortPathSegment {
                                                PathSegmentType = 0x01,
                                                OptionalLinkAddressSize = null,
                                                OptionalExtendedPortIdentifier = null,
                                                LinkAddress = new byte[] { 0 },
                                                Pad = null
                                            }
                                        }
                                    }
                                };
                                msg.CIPConnectionManagerUnconnSnd.Pad = (msg.CIPConnectionManagerUnconnSnd.SizeOf() % 2) == 0 ? null : (byte?)0;
                                msg.CIPConnectionManagerUnconnSnd.RoutePathSize = (byte)(msg.CIPConnectionManagerUnconnSnd.RoutePath.Sum(a => a.SizeOf()) / 2);
                                msg.CIPConnectionManagerUnconnSnd.CommonIndustrialProtocolRequest.RequestPathSize = (byte)(msg.CIPConnectionManagerUnconnSnd.CommonIndustrialProtocolRequest.PathSegmentList.Sum(a => a.SizeOf()) / 2);
                                msg.CIPConnectionManagerUnconnSnd.MessageRequestSize = (ushort)(msg.CIPConnectionManagerUnconnSnd.CommonIndustrialProtocolRequest.SizeOf() + msg.CIPConnectionManagerUnconnSnd.CIPClassGeneric.SizeOf());
                                msg.CommandSpecificDataSendRRData.List[1].Length = (ushort)(msg.CommonIndustrialProtocolRequest.SizeOf() + msg.CIPConnectionManagerUnconnSnd.SizeOf());
                                header.Length = msg.SizeOf();
                                SendMessage(header, msg);
                            }
                            else
                            {
                                CommandEtherNetIPHeader msgListServices = new CommandEtherNetIPHeader { Command = EncapsulationCommands.NOP };
                                SendMessage(msgListServices, null);
                            }
                            break;
                        }
                    }
                    if (!m_Terminate)
                        m_ThreadResetEvent.WaitOne(2000);
                }
            }
            catch (Exception exc)
            {
                Trace(EventType.Error, string.Format("{0} - Thread exception: {1}", LOG_TAG, exc.Message));
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
            m_SocketClient.SendData(allBytes);
        }

        private void SocketClient_OnReceiveData(byte[] data)
        {
            try
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
                            try
                            {
                                MessageFactory(headerStructObj, bodyBytes);
                            }
                            catch (Exception e)
                            {
                                Trace(EventType.Error, string.Format("{0} - Exception in msg. factory", LOG_TAG, data.Length));
                                Trace(e);
                            }
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
            catch (Exception e)
            {
                Trace(EventType.Error, string.Format("{0} - Exception in receive data function: {1}", LOG_TAG, e.Message));
                Trace(e);
            }
        }

        private void MessageFactory(CommandEtherNetIPHeader header, byte[] bodyBytes)
        {
            int pointer = 0;
            Trace(EventType.Info, string.Format("{0} - Receive msg. '{1}'", LOG_TAG, header.Command));
            long headerSize = Marshal.SizeOf(typeof(CommandEtherNetIPHeader));            
            switch (header.Command)
            {
                case EncapsulationCommands.ListServices:
                {                  
                    if (bodyBytes.Length > 0)
                    {
                        MsgListServiceReply msgReply = (MsgListServiceReply)MsgListServiceReply.Deserialize(typeof(MsgListServiceReply), bodyBytes, ref pointer);
                        if ((msgReply.CommandSpecificDataListServices.Items[0].CapabilityFlags & 0x20) == 0)
                            throw new Exception("The order side doesn't support TCP messages");
                        m_ClientConnStates = ClientConnStates.SendRegisterSession;
                    }
                    break;
                }

                case EncapsulationCommands.RegisterSession:
                {
                    if (header.Status == 0 && header.SessionHandle != 0)
                    {
                        MsgRegisterSessionReply msgReply = (MsgRegisterSessionReply)MsgListServiceReply.Deserialize(typeof(MsgRegisterSessionReply), bodyBytes, ref pointer);
                        Trace(EventType.Info, string.Format("{0} - Registration session number: {1}", LOG_TAG, header.SessionHandle));
                        m_SessionHandle = header.SessionHandle;
                        m_ClientConnStates = ClientConnStates.SendReceive;
                    }
                    else
                        throw new Exception(string.Format("Error on registration session. Error: {0}", header.Status));
                    break;
                }

                case EncapsulationCommands.SendRRData:
                {
                    MsgUnconnectedSendReply msgReply = (MsgUnconnectedSendReply)MsgUnconnectedSendReply.Deserialize(typeof(MsgUnconnectedSendReply), bodyBytes, ref pointer);
                    long sendContext = BitConverter.ToInt64(header.SenderContext, 0);
                    if (sendContext != 0 && sendContext == m_SenderContext)
                    {
                        m_SendStatusResult = msgReply.CommonIndustrialProtocolReply.GeneralStatus;
                        m_SendResetEvent.Set();
                    }
                    break;
                }

                case EncapsulationCommands.UnRegisterSession:
                {                    
                    break;
                }

                default:
                {
                    throw new Exception(string.Format("Command {0} not implemented", header.Command));
                }
            }
        }      

        public void SendData(string symbol, ElementaryDataType dataType, byte[] dataBytes)
        {
            lock (m_SndMutex)
                if (m_SocketClient.Connected)
                {
                    if (m_ClientConnStates != ClientConnStates.SendReceive)
                        throw new Exception("Waiting registration...");
                    m_SendResetEvent.Reset();
                    m_SenderContext = DateTime.Now.Ticks;
                    m_Symbol = symbol;
                    m_DataToSend = dataBytes;
                    m_DataTypeToSend = dataType;
                    m_SendStatusResult = null;
                    m_ThreadResetEvent.Set();
                    try
                    {
                        if (!m_SendResetEvent.WaitOne(SEND_TIME_OUT))
                        {
                            m_SocketClient.TryToReconnect();
                            throw new Exception("Send time-out! The connection will be closed.");
                        }
                        else
                        {
                            if (m_SendStatusResult.HasValue)
                            {
                                if (m_SendStatusResult.Value != 0)
                                    throw new Exception(string.Format("Send error code: {0}", m_SendStatusResult.Value));
                            }
                            else
                            {
                                m_SocketClient.TryToReconnect();
                                throw new Exception("Send failed! The connection will be closed.");
                            }
                        }
                    }
                    finally
                    {
                        m_DataToSend = null;
                    }
                }
                else
                    throw new Exception("Could not send data. Send connection not established yet.");
        }

        private void SocketClient_OnConnect(SocketConn scktConn)
        {
            m_ClientConnStates = ClientConnStates.SendListServices;
            Trace(EventType.Info, string.Format("{0} - Connection established: {0}", LOG_TAG, scktConn.RemoteEndPoint));
            OnConnStatusChanged?.Invoke(ConnType.Send, true, scktConn.ConnID);
        }

        private void SocketClient_OnDisconnect(SocketConn scktConn)
        {
            m_ClientConnStates = ClientConnStates.Disconnected;
            Trace(EventType.Info, string.Format("{0} - Connection is closed: {0}", LOG_TAG, scktConn.RemoteEndPoint));
            OnConnStatusChanged?.Invoke(ConnType.Send, false, scktConn.ConnID);
        }

        private void SocketClient_OnConnectError(Exception scktExp)
        {            
            Trace(EventType.Error, string.Format("{0} - Error during client connection establishment: {0}", LOG_TAG, scktExp.Message));
        }

        private void SocketClient_OnReceiveError(Exception scktExp)
        {            
            Trace(EventType.Error, string.Format("{0} - Error on receiving data from client connection: {0}", LOG_TAG, scktExp.Message));
        }

        private void SocketClient_OnSendError(Exception scktExp)
        {   
            Trace(EventType.Error, string.Format("{0} - Error on sending data from client connection: {0}", LOG_TAG, scktExp.Message));         
        }

        private void SocketServer_OnConnect(SocketConn scktConn)
        {            
            lock (m_CIPConnList)
            {
                CIPConn cipConn = new CIPConn(scktConn);
                cipConn.OnEventTrace += CIPConn_OnEventTrace;               
                cipConn.OnReceiveData += OnConnRecReceivedDataMsg;
                cipConn.Open();
                m_CIPConnList.Add(scktConn, cipConn);
            }
            OnConnStatusChanged?.Invoke(ConnType.Receive, true, scktConn.ConnID);            
        }

        private void SocketServer_OnDisconnect(SocketConn scktConn)
        {           
            lock (m_CIPConnList)
                if (m_CIPConnList.ContainsKey(scktConn))
                    m_CIPConnList.Remove(scktConn);
            OnConnStatusChanged?.Invoke(ConnType.Receive, false, scktConn.ConnID);             
        }

        private void OnConnRecReceivedDataMsg(string remoteEndPoint, string symbol, ElementaryDataType dataType, byte[] data)
        {
            try
            {
                OnConnRecReceiveMsgData?.Invoke(remoteEndPoint, symbol, dataType, data);
            }
            catch (Exception e)
            {
                Trace(e);
            }
        }

        private void CIPConn_OnEventTrace(EventType type, string message)
        {
            Trace(type, message);
        }

        private void SocketClient_OnEventTrace(EventType type, string message)
        {
            Trace(type, message);
        }

        private void SocketServer_OnEventTrace(EventType type, string message)
        {
            Trace(type, message);
        }
    }
}