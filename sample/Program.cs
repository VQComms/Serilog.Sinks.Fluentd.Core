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
            log.Information("Test");
        }
    }
}
