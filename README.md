# About
Driver to communicate with Rockwell PLCs (ControlLogix family) using CIP protocol over Ethernet/IP. This library was developed in .NET Core 2.0, supporting Linux/Windows cross platform.

# Features
- When the CIP class is instantiated, one outcoming connection is established with the PLC to send messages to PLC, working like a send channel (data from driver to PLC). A socket server is created to handle incoming connections, receive channel (data from PLC to driver). Each 'MSG' instruction block initialize a receiving connection in the driver side; 
- Use explicit message implementation: SendRRData;
- Use only TCP protocol;
- A sample program is located in 'Program.cs' file;
- NOP message implementation for all connection. Watchdog/life message not necessary in the application level;
- Many data types supported: BOOL, SINT, INT, DINT, LINT, USINT, UINT, UDINT, etc;
- To receive data in the PLC you have to do nothing else then configure a 'Controller Tag' with 'Read/Write' for 'External Access';

# Attention points
- The 'Connected' checkbox in the 'MSG' instruction block ('Connection' tab) MUST be unchecked. The Driver doesn't implement 'Connecting Manager - Forward Open'. Supporting only 'Unconnected messages'. Don't worry about the performance. It does not affect it. The socket connection remains established even when is not sending messages. The NOP message was implemented (working like a life or watchdog message).
- Receiving data in PLC: in some scenarios we have a higher traffic of data from Driver to PLC and a received data could be overwriting before it is treated. In these cases, we recommend that an ACK message is implemented for data flow control (from PLC to Driver);
- To use a different port in the driver side (the default port is 44818), just change de 'Path' field in the MSG instruction block windows property like this: 'ENET1, 2, 192.168.91.182:44820, 1, 0'. Now the new port is 44820. Explaining 'Path' field: ENET1 is a summary of the first backplane and slot (1, 4 – in my case). The second attribute is always 2 (to go outside to the Ethernet). The third attribute is the IP address followed by the port. The last two attributes are the remote backplane and slot (could be anything, the Driver doesn’t check these attributes);

# PLC Program Example
![Alt text](plc_program_1.PNG)
![Alt text](plc_program_2.PNG)
![Alt text](plc_program_3.PNG)
![Alt text](plc_program_4.PNG)
