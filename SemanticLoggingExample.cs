using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using SemanticLogging.LogglySink;
using System;
using System.Diagnostics.Tracing;
using System.Threading;

namespace SemanticLoggingTest
{
    class Program
    {
        static void Main(string[] args)
        {
            /**
             *  Setup for in-process logging. Do it once for the application setup.
             */ 
            var listener1 = new ObservableEventListener();
            listener1.EnableEvents(
              LogEventSource.Log, EventLevel.LogAlways,
              LogEventSource.Keywords.Perf | LogEventSource.Keywords.Diagnostic);

            // When setting up the the listener we can override the default values for buffering. Default values are recommended unless you are testing or have specific needs.
            var sinkSubscription = listener1.LogToLoggly("TestInstance", "https://logs-01.loggly.com", "[Customer token]", "LogglyTest",
                TimeSpan.FromMinutes(1), null, 5, 1000);


            /**
             * Do some logging ...
             */
            Console.WriteLine("Start...");

            // Log startup event ...
            LogEventSource.Log.Startup();

            // Log some fake failures ...
            for (int i = 0; i < 2; i++)
            {
                LogEventSource.Log.Failure("fail!!!");
                Thread.Sleep(500);
            }


            /**
             * Dispose or flush the sink at the end of the process when using in-process logging.
             * Otherwise, you could lose logging data.
             * Default settings will flush the buffer at 10 minute intervals or when buffering count is reached, i.e. 1000.
             */
            sinkSubscription.Sink.FlushAsync();

            Console.WriteLine("Press any key to quit.");
            Console.ReadKey();

        }
    }

    [EventSource(Name = "LogTestSource")]
    public class LogEventSource : EventSource
    {
        public class Keywords
        {
            public const EventKeywords Page = (EventKeywords)1;
            public const EventKeywords DataBase = (EventKeywords)2;
            public const EventKeywords Diagnostic = (EventKeywords)4;
            public const EventKeywords Perf = (EventKeywords)8;
        }

        public class Tasks
        {
            public const EventTask Page = (EventTask)1;
            public const EventTask DBQuery = (EventTask)2;
        }

        private static LogEventSource _log = new LogEventSource();
        private LogEventSource() { }
        public static LogEventSource Log { get { return _log; } }

        [Event(1, Message = "Application Failure: {0}",
        Level = EventLevel.Critical, Keywords = Keywords.Diagnostic,
        Opcode = EventOpcode.Info)]
        internal void Failure(string message)
        {
            this.WriteEvent(1, message);
        }

        [Event(2, Message = "Starting up.", Keywords = Keywords.Perf,
        Level = EventLevel.Informational,
        Opcode = EventOpcode.Info)]
        internal void Startup()
        {
            this.WriteEvent(2);
        }

        [Event(3, Message = "loading page {1} activityID={0}",
        Opcode = EventOpcode.Start,
        Task = Tasks.Page, Keywords = Keywords.Page,
        Level = EventLevel.Informational)]
        internal void PageStart(int ID, string url)
        {
            if (this.IsEnabled()) this.WriteEvent(3, ID, url);
        }
    }
}
