namespace Serilog.Sinks.Fluentd.Core.Sinks
{
    public class LocalException
    {
        public int Depth { get; set; }

        public string Message { get; set; }

        public string Source { get; set; }

        public string StackTraceString { get; set; }

        public int HResult { get; set; }

        public string HelpURL { get; set; }
    }
}
