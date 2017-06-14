namespace Serilog.fluentd
{
    public class FluentdMessage
    {
        public class InternalMessage
        {
            public string tag { get; set; }
            public long time { get; set; }
            public string record { get; set; }

            public InternalMessage() { }
        }
        public InternalMessage Message { get; set; }

        public FluentdMessage(string tag, long time, string record)
        {
            this.Message = new InternalMessage();
            this.Message.tag = tag;
            this.Message.time = time;
            this.Message.record = record;
        }
    }
}