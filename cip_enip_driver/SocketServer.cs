// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Class: SocketServer
// Description: Represent a socket server (used for incoming connections)
// -----------------------------------------------------------------------------------------------------------------
using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace Techsteel.Drivers.CIP
{
    public class SocketServer : Traceable
    {
        private Socket m_IncomingSocket;
        private string m_LocalAddress;
        private int m_LocalPort;
        private Thread m_LocalThread = null;
        private ConcurrentDictionary<SocketConn, SocketConn> m_ConnList = new ConcurrentDictionary<SocketConn, SocketConn>();
        private bool m_Terminate = false;
        private bool m_StartScktConnReadThread = true;
        private EventWaitHandle m_AcceptDelayWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        private bool m_AcceptDelay = false;
        public int MaxConn { get; set; }

        private int MAX_PENDING_CONN = 100;
        private int ACCEPT_DELAY = 500;

        public delegate void DlgConnect(SocketConn scktConn);
        public delegate void DlgDisconnect(SocketConn scktConn);
        public delegate void DlgReceiveData(SocketConn scktConn, byte[] data);
        public delegate void DlgReceiveError(SocketConn scktConn, Exception scktExp);
        public delegate void DlgSendError(SocketConn scktConn, Exception scktExp);

        public event DlgConnect OnConnect;
        public event DlgDisconnect OnDisconnect;
        public event DlgReceiveData OnReceiveData;
        public event DlgReceiveError OnReceiveError;
        public event DlgSendError OnSendError;

        public SocketServer(string listenAddress, int listenPort)
            : this(listenAddress, listenPort, true, false) { }

        public SocketServer(string listenAddress, int listenPort, bool startScktConnReadThread, bool acceptDelay)
        {
            m_LocalAddress = listenAddress;
            m_LocalPort = listenPort;
            m_StartScktConnReadThread = startScktConnReadThread;
            m_AcceptDelay = acceptDelay;
        }

        public void Open()
        {
            if (m_IncomingSocket == null)
            {
                m_IncomingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                if (string.IsNullOrEmpty(m_LocalAddress))
                    m_IncomingSocket.Bind(new IPEndPoint(IPAddress.Any, m_LocalPort));
                else
                    m_IncomingSocket.Bind(new IPEndPoint(IPAddress.Parse(m_LocalAddress), m_LocalPort));
                m_IncomingSocket.Listen(MAX_PENDING_CONN);
                m_LocalThread = new Thread(ThreadTask);
                m_LocalThread.Start();
            }
        }

        public void TerminateListen()
        {
            m_Terminate = true;
            m_IncomingSocket?.Shutdown(SocketShutdown.Both);
            m_IncomingSocket?.Close();
            m_AcceptDelayWaitHandle.Set();
        }

        public void CloseAllConnections()
        {
            try
            {
                foreach (var kvp in m_ConnList)
                {
                    if (m_ConnList.TryRemove(kvp.Key, out SocketConn removed))
                    {
                        Trace(EventType.Data,
                            "Removed connection {0} from the socket server connection list",
                            removed);
                        removed.Close();
                    }
                    else
                        Trace(EventType.Exception,
                            "Removing connection from the socket server connection list: {0}",
                            kvp.Key);
                }
            }
            catch (Exception exc) { Trace(exc); }
        }

        public void CloseConnection(SocketConn scktConn)
        {
            scktConn.Close();
            if (m_ConnList.TryRemove(scktConn, out SocketConn removed))
                Trace(EventType.Data,
                    "Removed connection {0} from the socket server connection list",
                    removed);
            else
                Trace(EventType.Exception,
                    "Removing connection from the socket server connection list: {0}",
                    scktConn);
        }

        public string LocalEndPoint
        {
            get { return string.Format("{0}:{1}", m_LocalAddress, m_LocalPort); }
        }

        public SocketConn[] ConnectionList
        {
            get { return m_ConnList.Values.ToArray(); }
        }

        public bool SendData(SocketConn scktConn, byte[] data)
        {
            return scktConn.SendData(data);
        }

        public bool SendData(byte[] data)
        {
            bool result = true;
            foreach (SocketConn scktConn in ConnectionList)
                result &= scktConn.SendData(data);
            return result;
        }

        private void SocketConn_Disconnect(SocketConn scktConn)
        {
            if (m_ConnList.TryRemove(scktConn, out SocketConn removed))
                Trace(EventType.Data,
                    "Removed connection {0} from the socket server connection list by disconnection event",
                    removed);
            else
                Trace(EventType.Exception,
                    "Removing connection {0} from the socket server connection list by disconnection event",
                    scktConn);

            OnDisconnect?.Invoke(scktConn);
        }

        private void SocketConn_ReceiveData(SocketConn scktConn, byte[] data)
        {
            OnReceiveData?.Invoke(scktConn, data);
        }

        private void SocketConn_ReceiveError(SocketConn scktConn, Exception scktExp)
        {
            OnReceiveError?.Invoke(scktConn, scktExp);
        }

        private void SocketConn_SendError(SocketConn scktConn, Exception scktExp)
        {
            OnSendError?.Invoke(scktConn, scktExp);
        }

        private void ThreadTask()
        {
            try
            {
                while (!m_Terminate)
                {
                    Socket sckt = null;
                    try
                    {
                        sckt = m_IncomingSocket.Accept();
                    }
                    catch (SocketException e)
                    {
                        Trace(e);
                        sckt = null;
                    }
                    if (sckt != null)
                    {
                        if (MaxConn == 0 ||
                            m_ConnList.Count < MaxConn)
                        {
                            var scktConn = new SocketConn(sckt, false);
                            scktConn.OnEventTrace += ScktConn_OnEventTrace;
                            scktConn.OnDisconnect += new SocketConn.DlgDisconnect(SocketConn_Disconnect);
                            scktConn.OnSendError += new SocketConn.DlgSendError(SocketConn_SendError);
                            if (!m_ConnList.TryAdd(scktConn, scktConn))
                            {
                                Trace(EventType.Exception,
                                    "Couldn't add connection to socket server connection list. Refusing connection. ConnID: {0}",
                                    scktConn);
                                sckt.Close();
                            }
                            OnConnect?.Invoke(scktConn);
                            if (m_StartScktConnReadThread)
                            {
                                scktConn.OnReceiveData += new SocketConn.DlgReceiveData(SocketConn_ReceiveData);
                                scktConn.OnReceiveError += new SocketConn.DlgReceiveError(SocketConn_ReceiveError);
                                scktConn.StartReadThread();
                            }
                            if (m_AcceptDelay)
                                m_AcceptDelayWaitHandle.WaitOne(ACCEPT_DELAY, false);
                        }
                        else
                        {
                            sckt.Close();
                            Trace(
                                EventType.Warning,
                                "Socket connection refused. Reached connection limit of {0} connections. {1}/{2}",
                                MaxConn,
                                sckt.LocalEndPoint,
                                sckt.RemoteEndPoint);
                        }
                    }
                }
            }
            catch (Exception exc) { Trace(exc); }
        }

        private void ScktConn_OnEventTrace(EventType type, string message)
        {
            Trace(type, message);
        }
    }
}
