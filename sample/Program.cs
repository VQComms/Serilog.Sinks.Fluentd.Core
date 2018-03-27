namespace sample
{
    using System;
    using System.Collections.Generic;
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

        public IEnumerable<int> SimpleList { get; set; }

        public IEnumerable<SubSub> ComplexList { get; set; }

        public Dictionary<string, object> ComplexDictionary { get; set; }
    }

    public class SubSub
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            Serilog.Debugging.SelfLog.Enable(Console.Error);

            var log = new LoggerConfiguration()
                .WriteTo.Fluentd(new FluentdHandlerSettings
                {
                    Tag = "My.SampleApp"
                })
                .CreateLogger();

            var info = new LogMessage
            {
                RequestId = "239423049FL",
                Component = "Startup",
                Method = "Configure",
                Message = "I did stuff",
                SimpleList = new[] { 9, 8, 7 },
                ComplexList = new[] { new SubSub { Id = 1, Name = "Vicent" }, new SubSub { Id = 2, Name = "Jules" } },
                ComplexDictionary = new Dictionary<string, object> { { "Id", 1 }, { "Name", "Fred" }, { "Sub", new SubSub { Id = 6, Name = "Joe" } } }
            };

            log.Information("{@info}", info);

            DoSomethingThatThrows(log);

            Thread.Sleep(60000);
        }

        private static void DoSomethingThatThrows(Logger log)
        {
            try
            {
                throw new NullReferenceException("Ooh it broke", new ArgumentException("Busted"));
            }
            catch (Exception e)
            {
                log.Error(e, "{@info}",
                    new LogMessage
                    {
                        RequestId = "239423049FL",
                        Component = "Startup",
                        Method = "Configure",
                        Message = "There has been an error",
                        SimpleList = new[] { 1, 2, 3 },
                        ComplexList = new[] { new SubSub { Id = 3, Name = "Mia" }, new SubSub { Id = 4, Name = "Marcellus" } },
                        ComplexDictionary = new Dictionary<string, object>() { { "Id", 2 }, { "Name", "Talula" }, { "Sub", new SubSub { Id = 9, Name = "Jim" } } }
                    });
            }
        }
    }
}
