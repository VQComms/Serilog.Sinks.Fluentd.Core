using System;
using Serilog.Configuration;
using Serilog.Sinks.SystemConsole;

namespace Serilog
{
    public static class FluentdLoggerConfigurationExtensions
    {
        public static LoggerConfiguration Fluentd(this LoggerSinkConfiguration sinkConfiguration)
        {
            if (sinkConfiguration == null)
            {
                throw new ArgumentNullException(nameof(sinkConfiguration));
            }
            return sinkConfiguration.Sink(new FluentdSink());
        }
    }
}