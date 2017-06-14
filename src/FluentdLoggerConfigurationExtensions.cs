using System;
using Serilog.Configuration;
using Serilog.Sinks.SystemConsole;

namespace Serilog
{
    public static class FluentdLoggerConfigurationExtensions
    {
        public static LoggerConfiguration Fluentd(this LoggerSinkConfiguration sinkConfiguration,
                string tag = "",
                string hostname = "localhost",
                int port = 24224,
                int timeout = 3000)
        {
            if (sinkConfiguration == null)
            {
                throw new ArgumentNullException(nameof(sinkConfiguration));
            }
            return sinkConfiguration.Sink(new FluentdSink(tag, hostname, port, timeout));
        }
    }
}