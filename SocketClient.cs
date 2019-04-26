// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Class: SocketClient
// Description: Represent a socket client (used for outcoming connections)
// -----------------------------------------------------------------------------------------------------------------
using System;
using System.Threading;
using System.Net.Sockets;

namespace Techsteel.Drivers.CIP
{
    public class SocketClient
    {
        private SocketConn m_SocketConn = null;
        private string m_RemoteAddress = null;
        private int m_RemotePort;
        private bool m_StartScktConnReadThread = false;
        private Thread m_LocalThread = null;
        private bool m_Terminate = false;
        private ManualResetEvent m_ReconnectSignal = new ManualResetEvent(false);

        private int RETRY_CONNECT = 5000;

        public delegate void DlgConnect(SocketConn scktConn);
        public delegate void DlgDisconnect(SocketConn scktConn);
        public delegate void DlgConnectError(Exception scktExp);
        public delegate void DlgReceiveData(byte[] data);
        public delegate void DlgReceiveError(Exception scktExp);
        public delegate void DlgSendError(Exception scktExp);

        public event DlgConnect OnConnect;
        public event DlgDisconnect OnDisconnect;
        public event DlgConnectError OnConnectError;
        public event DlgReceiveData OnReceiveData;
        public event DlgReceiveError OnReceiveError;
        public event DlgSendError OnSendError;

        public SocketClient(string remoteAddress, int remotePort)
            : this(remoteAddress, remotePort, true) { }

        public SocketClient(string remoteAddress, int remotePort, bool startScktConnReadThread)
        {
            m_RemoteAddress = remoteAddress;
            m_RemotePort = remotePort;
            m_StartScktConnReadThread = startScktConnReadThread;
        }

        public void Open()
        {
            if (m_LocalThread == null)
            {
                m_LocalThread = new Thread(ThreadTask);
                m_LocalThread.Start();
            }
        }

        public void Close()
        {
            Close(false);
        }

        public void Close(bool keepCurrConn)
        {
            EventTracer.Trace(EventTracer.EventType.Data,
                "Closing socket client {0}",
                RemoteEndPoint);
            m_Terminate = true;
            m_ReconnectSignal.Set();
            if (!keepCurrConn)
                m_SocketConn?.Close();
        }

        public string RemoteEndPoint
        {
            get { return string.Format("{0}:{1}", m_RemoteAddress, m_RemotePort); }
        }

        public bool Connected
        {
            get { return m_SocketConn != null && m_SocketConn.Connected; }
        }

        public bool SendData(byte[] data)
        {
            if (Connected)
                return m_SocketConn.SendData(data);
            else
                return false;
        }

        public void TryToReconnect()
        {
            try
            {
                m_SocketConn?.Close();
            }
            catch (Exception exc)
            {
                EventTracer.Trace(exc);
            }
            finally
            {
                m_SocketConn = null;
            }
        }

        private void SocketConn_Disconnect(SocketConn scktConn)
        {
            if (!m_Terminate)
            {
                TryToReconnect();
                OnDisconnect?.Invoke(scktConn);
            }
        }

        private void SocketConn_ReceiveData(SocketConn scktConn, byte[] data)
        {
            OnReceiveData?.Invoke(data);
        }

        private void SocketConn_ReceiveError(SocketConn scktConn, Exception scktExp)
        {
            OnReceiveError?.Invoke(scktExp);
        }

        private void SocketConn_SendError(SocketConn scktConn, Exception scktExp)
        {
            OnSendError?.Invoke(scktExp);
        }

        private void ThreadTask()
        {
            try
            {
                while (!m_Terminate)
                {
                    if (m_SocketConn == null || !m_SocketConn.Connected)
                    {
                        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        try
                        {
                            socket.Connect(m_RemoteAddress, m_RemotePort);
                            m_SocketConn = new SocketConn(socket, false);
                            m_SocketConn.OnDisconnect += new SocketConn.DlgDisconnect(SocketConn_Disconnect);
                            m_SocketConn.OnSendError += new SocketConn.DlgSendError(SocketConn_SendError);
                            if (m_StartScktConnReadThread)
                            {
                                m_SocketConn.OnReceiveData += new SocketConn.DlgReceiveData(SocketConn_ReceiveData);
                                m_SocketConn.OnReceiveError += new SocketConn.DlgReceiveError(SocketConn_ReceiveError);
                                m_SocketConn.StartReadThread();
                            }
                            OnConnect?.Invoke(m_SocketConn);
                        }
                        catch (Exception e)
                        {
                            EventTracer.Trace(EventTracer.EventType.Error,
                                "Exception connecting the socket. Remote endpoint: {0}",
                                RemoteEndPoint);
                            OnConnectError?.Invoke(e);
                            TryToReconnect();
                        }
                    }
                    m_ReconnectSignal.WaitOne(RETRY_CONNECT);
                }
            }
            catch (Exception exc)
            {
                EventTracer.Trace(exc);
            }
        }
    }
}
