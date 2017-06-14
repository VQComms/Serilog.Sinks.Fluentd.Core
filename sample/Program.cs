using Serilog;

namespace sample
{
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

            var info = new { RequestId = "239423049FG", Component = "Startup", Method = "Configure", Message = "I did stuff" };

            log.Information("Foo.bar {@info}", info);

        }
    }
}
