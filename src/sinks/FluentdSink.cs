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
        public FluentdSink()
        {
            handler = FluentdHandler.CreateHandler("test", new FluentdHandlerSettings()
            {
                Host = "localhost",
                Port = 24224,
                MaxBuffer = 1024 * 1024 * 10,
                Tag = "VQ"
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
