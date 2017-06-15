using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Fluentd.Core.Fluentd;

namespace Serilog.Sinks.Fluentd.Core.Sinks
{
    public class FluentdSink : ILogEventSink
    {
        private readonly FluentdHandler handler;
        public FluentdSink(string tag = "", string hostname = "localhost", int port = 24224, int timeout = 3000)
        {
            handler = FluentdHandler.CreateHandler("test", new FluentdHandlerSettings()
            {
                Host = hostname,
                Port = port,
                Tag = tag,
                Timeout = timeout
            }).Result;
        }

        public void Emit(LogEvent logEvent)
        {
            handler.Send(logEvent).Wait();
        }
    }
}
