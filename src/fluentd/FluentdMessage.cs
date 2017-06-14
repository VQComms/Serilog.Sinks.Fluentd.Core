namespace Serilog.fluentd
{
    public class FluentdMessage
    {
        public string Message { get; set; }

        public FluentdMessage(string record)
        {
            this.Message = record;
        }
    }
}