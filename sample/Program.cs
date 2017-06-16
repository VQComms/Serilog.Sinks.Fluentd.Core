using Serilog;
using Serilog.Sinks.Fluentd.Core;

namespace sample
{
    public class LogMessage
    {
        public string RequestId { get; set; }
        public string Component { get; set; }

        public string Method { get; set; }

        public string Message { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var log = new LoggerConfiguration()
                .WriteTo.Fluentd(
                    tag: "My.SampleApp",
                    timeout: 4000,
                    hostname: "localhost",
                    port: 24224)
                .CreateLogger();

            var info = new LogMessage { RequestId = "239423049FL", Component = "Startup", Method = "Configure", Message = "I did stuff" };

            log.Information("{@info}", info);
        }
    }
}