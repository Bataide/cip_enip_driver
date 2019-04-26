// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Class: Program
// Description: Sample program explaining how to use the library
// -----------------------------------------------------------------------------------------------------------------
using System;
using System.Linq;
using System.Threading;

namespace Techsteel.Drivers.CIP
{
    class Program
    {
        static byte[] m_BytesToSend = null;
        static CIP m_Cip;

        static void Main(string[] args)
        {
            // Set the log level
            EventTracer.LogLevel = EventTracer.EventType.Info;

            // Create CIP communitation object. The first parameter is the local IP address to
            // listen incoming connection in default port 0xAF12 (from 'MSG' block in ladder program).
            // The second parameter is the PLC's IP addres using the same default port (0xAF12, don't change it.
            // PLC's listen only in this port)
            // PS: the 'Connected' checkbox in the 'MSG' instruction block property ('Connection' tab) MUST be unchecked.
            // Don't worry, this doesn't affect the communication performance. The socket connection remains estabilished even 
            // when is not sending messages. The NOP messagem was implemented (working like a life/watchdog msg).
            m_Cip = new CIP("0.0.0.0", "192.168.91.168");
            m_Cip.OnConnRecReceiveMsgData += OnConnRecReceiveMsgData; // <- event to receive data from incoming connections
            m_Cip.OnConnStatusChanged += OnConnStatusChanged; // <- event to get connection state changes
            m_Cip.Open();

            // Initialize variable to be send
            m_BytesToSend = new byte[256];
            
            while (true)
            {
                // If the send connection is estabilished and ready to send...
                if (m_Cip.SendChannelConnected() && m_Cip.ReadyToSend())
                {
                    try
                    {
                        // Change the message content
                        m_BytesToSend[0]++;
                        for (int i = 1; i < m_BytesToSend.Length; i++)
                            m_BytesToSend[i] = (byte)(m_BytesToSend[i - 1] + 1);

                        // Log.
                        EventTracer.Trace(EventTracer.EventType.Info, string.Format("Sending data ({0} bytes)...", m_BytesToSend.Length));

                        // Write data in the 'RECEIVE' tag in the PLC ('RECEIVE' is the tag's name)
                        // You have to create a tag with this exactly name, array of SINT type (one byte), total size 300
                        m_Cip.SendData("RECEIVE_TAG", ElementaryDataType.SINT, m_BytesToSend);

                        // Success log
                        string strData = string.Join("|", m_BytesToSend.Select(a => a.ToString()));
                        EventTracer.Trace(EventTracer.EventType.Info, string.Format("Send msg. successful ({0} bytes)... [{1}]", m_BytesToSend.Length, strData));
                    }
                    catch (Exception e)
                    {
                        // Error log
                        EventTracer.Trace(EventTracer.EventType.Exception, string.Format("Fail to send data. Error: {0}", e.Message));
                    }
                }

                // Delay
                Thread.Sleep(1000);
            }
        }

        // Function to receive data from incoming connections
        // param "remoteEndPoint" -> IP/port from the remote side (used to distingue multiples incoming connections)
        // param "symbol"         -> the tag name
        // param "dataType"       -> data type (SINT for example)
        // param "data"           -> data
        private static void OnConnRecReceiveMsgData(string remoteEndPoint, string symbol, ElementaryDataType dataType, byte[] data)
        {
            // Log
            string strData = string.Join("|", data.Select(a => a.ToString()));
            EventTracer.Trace(EventTracer.EventType.Info, string.Format("Data received ({0} bytes). Tag: {1} Type: {2} Data: [{3}]", data.Length, symbol, dataType, strData));
        }

        // Function to notify connections state changes
        // param "connType"  -> distingue between outcoming and incoming connections
        // param "connected" -> the tag name
        // param "connID"    -> data type (SINT for example)
        private static void OnConnStatusChanged(CIP.ConnType connType, bool connected, string connID)
        {
            // Log
            EventTracer.Trace(EventTracer.EventType.Info, string.Format("Connections state changed. ConnType: {0} IsConnected: {1} ConnID: {2}",
                connType,
                connected ? "YES" : "NO",
                connID));
        }
    }
}
