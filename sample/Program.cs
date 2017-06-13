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
            log.Information("Sample Log Entry");
            log.Information("Sample Log Entry");
            log.Information("Sample Log Entry");
            log.Information("Sample Log Entry");
            log.Information("Sample Log Entry");
            log.Information("Sample Log Entry");
            log.Information("Sample Log Entry");
            log.Information("Sample Log Entry");
            log.Information("Sample Log Entry");

        }
    }
}
