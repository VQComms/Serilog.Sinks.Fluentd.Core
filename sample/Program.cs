namespace sample
{
    using System;
    using System.Threading;
    using Serilog;
    using Serilog.Core;
    using Serilog.Sinks.Fluentd.Core;

    public class LogMessage
    {
        public string RequestId { get; set; }

        public string Component { get; set; }

        public string Method { get; set; }

        public string Message { get; set; }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            var log = new LoggerConfiguration()
                .WriteTo.Fluentd(new FluentdHandlerSettings
                {
                    Tag = "My.SampleApp"
                })
                .CreateLogger();

            var info = new LogMessage { RequestId = "239423049FL", Component = "Startup", Method = "Configure", Message = "I did stuff" };

            log.Information("{@info}", info);

            DoSomethingThatThrows(log);

            Thread.Sleep(10000);
        }

        private static void DoSomethingThatThrows(Logger log)
        {
            try
            {
                throw new NullReferenceException("Ooh it broke", new ArgumentException("Busted"));
            }
            catch (Exception e)
            {
                log.Error(e, "{@info}", new LogMessage { RequestId = "239423049FL", Component = "Startup", Method = "Configure", Message = "There has been an error" });
            }
        }
    }
}
