// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Class: SocketConn
// Description: Represent a socket connection
// -----------------------------------------------------------------------------------------------------------------
using System;
using System.Threading;
using System.Net.Sockets;

namespace Techsteel.Drivers.CIP
{
    public class SocketConn : Traceable
    {
        private Socket m_Socket;
        private string m_RemoteEndPoint;
        private string m_LocalEndPoint;
        private Thread m_ReceiveThread = null;
        private byte[] m_Buffer = new byte[BUFFER_SIZE];
        private Mutex m_SendMutex = new Mutex();
        private bool m_Terminate = false;
        private object m_LockClose = new object();

        private const int BUFFER_SIZE = 1024 * 16;
        private const int SEND_TIME_OUT = 30000;

        internal delegate void DlgDisconnect(SocketConn scktConn);
        internal delegate void DlgReceiveData(SocketConn scktConn, byte[] data);
        internal delegate void DlgReceiveError(SocketConn scktConn, Exception scktExp);
        internal delegate void DlgSendError(SocketConn scktConn, Exception scktExp);

        internal event DlgDisconnect OnDisconnect;
        internal event DlgReceiveData OnReceiveData;
        internal event DlgReceiveError OnReceiveError;
        internal event DlgSendError OnSendError;

        internal SocketConn(Socket socket) : this(socket, true) { }

        internal SocketConn(Socket socket, bool startReadThread)
        {
            m_Socket = socket ?? throw new ArgumentNullException("socket");
            m_RemoteEndPoint = m_Socket.RemoteEndPoint.ToString();
            m_LocalEndPoint = m_Socket.LocalEndPoint.ToString();
            m_Socket.SendTimeout = SEND_TIME_OUT;
            if (startReadThread)
                StartReadThread();
        }

        public void StartReadThread()
        {
            if (m_ReceiveThread == null)
            {
                m_ReceiveThread = new Thread(RecThreadTask);
                m_ReceiveThread.Start();
            }
        }

        public string RemoteEndPoint
        {
            get { return m_RemoteEndPoint; }
        }

        public string LocalEndPoint
        {
            get { return m_LocalEndPoint; }
        }

        public string ConnID
        {
            get { return string.Format("{0}/{1}", m_LocalEndPoint, m_RemoteEndPoint); }
        }

        public bool Connected
        {
            get { return m_Socket != null && m_Socket.Connected; }
        }

        internal void Close()
        {
            lock (m_LockClose)
            {
                if (!m_Terminate)
                    m_Terminate = true;
                else
                    return;
            }

            try
            {
                if (Connected)
                    m_Socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception exc)
            {
                Trace(EventType.Error,
                    "Error on socket shutdown {0}: {1}",
                    ConnID,
                    exc.Message);
            }

            bool socketclosed = false;
            if (m_ReceiveThread != null)
            {
                const int WAITS_OTHER_SIDE_CLOSING = 3000;
                if (!(Thread.CurrentThread.ManagedThreadId == m_ReceiveThread.ManagedThreadId) &&
                    !m_ReceiveThread.Join(WAITS_OTHER_SIDE_CLOSING))
                {
                    Trace(EventType.Exception,
                        "Forcefully closing socket as the other side didn't closed: {0}",
                            ConnID);
                    ClosesSocket();
                    socketclosed = true;
                }
                m_ReceiveThread = null;
            }
            if (!socketclosed)
                ClosesSocket();

            OnDisconnect?.Invoke(this);
        }

        private void ClosesSocket()
        {
            try
            {
                m_Socket?.Close();
            }
            catch (Exception exc){
                Trace(EventType.Error,
                    "Error closing socket {0}: {1}",
                    ConnID,
                    exc.Message);
            }
        }

        internal bool SendData(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            try
            {
                m_SendMutex.WaitOne();
                if (Connected)
                {
                    SocketError errorCode;
                    int bytesSent = m_Socket.Send(data, 0, data.Length, SocketFlags.None, out errorCode);
                    if (errorCode != SocketError.Success)
                    {
                        Trace(EventType.Exception,
                            "Socket send error {3}. ConnID: {0} Data.Length: {1} bytesSent: {2}",
                            ConnID,
                            data.Length,
                            bytesSent,
                            errorCode);
                        return false;
                    }
                    if (bytesSent < data.Length)
                    {
                        Trace(EventType.Exception,
                            "Not all data was putted on the socket buffer. ConnID: {0} Data.Length: {1} bytesSent: {2}",
                            ConnID,
                            data.Length,
                            bytesSent);
                        return false;
                    }
                    Trace(EventType.Full,
                        "Socket data sent. ConnID: {0} Length: {1} byte(s)",
                        ConnID,
                        data.Length);
                    return true;
                }
                else
                {
                    Trace(EventType.Exception,
                        "Can't send socket data on disconnected connections. ConnID: {0}",
                        ConnID);
                    return false;
                }
            }
            catch (Exception e)
            {
                Trace(EventType.Error,
                    "Exception sending socket data for ConnID {0}",
                    ConnID);
                try { OnSendError?.Invoke(this, e); }
                catch (Exception exc) { Trace(exc); }
                try
                {
                    Trace(EventType.Error,
                        "Starting socket connection closing because of sending error: {0}",
                        ConnID);
                    Close();
                }
                catch (Exception exc) { Trace(exc); }
                return false;
            }
            finally
            {
                m_SendMutex.ReleaseMutex();
            }
        }

        private void RecThreadTask()
        {
            try
            {
                while (!m_Terminate)
                {
                    if (m_Socket.Connected)
                    {
                        int byteCount = 0;
                        try
                        {
                            byteCount = m_Socket.Receive(m_Buffer);
                        }
                        catch (Exception e)
                        {
                            Trace(EventType.Error,
                                "Receiving socket data, connection will be closed: {0}",
                                ConnID);
                            Trace(e);
                            OnReceiveError?.Invoke(this, e);
                        }

                        if (byteCount > 0)
                        {
                            byte[] data = new byte[byteCount];
                            Array.Copy(m_Buffer, data, byteCount);
                            Trace(EventType.Full,
                                "Received socket data. ConnID: {0} Length: {1} byte(s)",
                                ConnID,
                                byteCount);
                            OnReceiveData?.Invoke(this, data);
                        }
                        else
                        {
                            Trace(EventType.Full,
                                "Closing socket connection (from the other side!). ConnID: {0}",
                                ConnID);
                            Close();
                        }
                    }
                    else
                    {
                        Trace(EventType.Full,
                            "Closing socket connection (!Connected). ConnID: {0}",
                            ConnID);
                        Close();
                    }
                }
                Trace(EventType.Full,
                    "Finished receiving thread. ConnID: {0}",
                    ConnID);
            }
            catch (Exception exc)
            {
                Trace(exc);
            }
        }

        public override string ToString()
        {
            return ConnID;
        }
    }
}
