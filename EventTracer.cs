// -----------------------------------------------------------------------------------------------------------------
// Project: CIP driver over Ethernet/IP - Copyright (C) 2019, Bruno Ataíde (Techsteel), All rights reserved
// License: GNU General Public License 3.0
// Public Repo: https://github.com/Bataide/cip_enip_driver
// Class: EventTracer
// Description: Used for console logging
// -----------------------------------------------------------------------------------------------------------------
using System;
using System.Text;

namespace Techsteel.Drivers.CIP
{
    public class EventTracer
    {
        public enum EventType
        {
            NoTrace = 0,
            Exception = 1,
            Error = 2,
            Warning = 3,
            Info = 4,
            Data = 5,
            Full = 6,
        }

        public static Action<EventType, string> TraceMethod = DefaultTraceMethod;

        public static EventType LogLevel { get; set; }

        public static string Header {get; set; }

        static EventTracer() { LogLevel = EventType.Full; }

        public static void Trace(Exception exception)
        {
            Trace(EventType.Exception, ExtractAllExceptions(exception));
        }

        private static string ExtractAllExceptions(Exception exc)
        {
            if (exc == null)
                return "Exception was null";
            var sb = new StringBuilder();
            sb.AppendFormat("{0} {1} {2}",
                exc.GetType(),
                exc.Message?.Replace(Environment.NewLine, " "),
                exc.StackTrace?.Replace(Environment.NewLine, " "));
            if (exc.InnerException != null)
                sb.AppendFormat(" Inner -> {0}", ExtractAllExceptions(exc.InnerException));
            return sb.ToString();
        }

        public static void Trace(EventType type, string message, params object[] args)
        {
            if (type <= LogLevel)
                Trace(type, string.Format(message, args));
        }

        public static void Trace(EventType type, string message)
        {
            DefaultTraceMethod(type, message);
        }

        private static void DefaultTraceMethod(EventType type, string message)
        {
            try
            {
                if (type <= LogLevel)
                    Console.WriteLine("{0:HH:mm:ss,fff} - {3} - {1,-9} - {2}",
                        DateTime.Now,
                        type,
                        message,
                        Header);
            }
            catch (Exception exc)
            {
                try
                {
                    Console.WriteLine("Trace (exception): {0}", ExtractAllExceptions(exc));
                }
                catch {}
            }
        }
    }
}
