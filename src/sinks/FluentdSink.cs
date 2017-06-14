using System;
using Serilog.Core;
using Serilog.Events;
using Serilog.fluentd;
using Serilog.Formatting.Display;
using static Serilog.fluentd.FluentdHandler;

namespace Serilog.Sinks.SystemConsole
{
    class FluentdSink : ILogEventSink
    {
        private readonly FluentdHandler handler;
        public FluentdSink(string tag = "", string hostname = "localhost", int port = 24224, int timeout = 3000)
        {
            handler = FluentdHandler.CreateHandler("test", new FluentdHandlerSettings()
            {
                Host = hostname,
                Port = port,
                Tag = tag,
                Timeout = 3000
            }).Result;
        }

        public void Emit(LogEvent logEvent)
        {
            var outputProperties = OutputProperties.GetOutputProperties(logEvent);

            try
            {
                handler.Emit(logEvent.RenderMessage(), outputProperties).Wait();
            }
            catch (Exception e)
            {

                Console.Error.WriteLine(e);
                throw e;
            }
        }
    }
}
