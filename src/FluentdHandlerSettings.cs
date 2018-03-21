namespace Serilog.Sinks.Fluentd.Core
{
    using System;

    public class FluentdHandlerSettings
    {
        public string Tag { get; set; } = "";

        public string Host { get; set; } = "localhost";

        public int Port { get; set; } = 24224;

        public int TCPSendTimeout { get; set; } = 3000;

        public TimeSpan BatchingPeriod { get; set; } = TimeSpan.FromSeconds(2);

        public int BatchPostingLimit { get; set; } = 50;

        public int TCPRetryAmount { get; set; } = 5;

        public int LingerTime { get; set; } = 5;
    }
}
