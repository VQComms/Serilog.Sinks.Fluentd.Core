using Serilog;

namespace sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var log = new LoggerConfiguration()
                .WriteTo.Fluentd()
                .CreateLogger();

            var info = new { RequestId = "239423049FG", Component = "Startup", Method = "Configure", Message = "I did stuff" };

            log.Information("Foo.bar {@info}", info);

        }
    }
}
