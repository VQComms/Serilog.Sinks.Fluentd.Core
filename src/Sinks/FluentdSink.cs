namespace Serilog.Sinks.Fluentd.Core.Sinks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog.Debugging;
    using Serilog.Events;
    using Serilog.Formatting.Json;
    using Serilog.Parsing;
    using Serilog.Sinks.PeriodicBatching;

    public class FluentdSink : PeriodicBatchingSink
    {
        private readonly FluentdHandlerSettings settings;

        public TcpClient client;

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public FluentdSink(FluentdHandlerSettings settings) : base(settings.BatchPostingLimit, settings.BatchingPeriod)
        {
            this.settings = settings;
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            await this.Send(events);
        }

        private async Task Connect()
        {
            if (this.client != null)
            {
                return;
            }

            this.client = new TcpClient { SendTimeout = this.settings.TCPSendTimeout };
            await this.client.ConnectAsync(this.settings.Host, this.settings.Port);
        }

        public async Task Send(IEnumerable<LogEvent> messages)
        {
            foreach (var logEvent in messages)
            {
                using (var sw = new StringWriter())
                {
                    this.Format(logEvent, sw);

                    var serialized = $"[\"{this.settings.Tag}\",{logEvent.Timestamp.ToUnixTimeSeconds()},{sw}]";
                    var encoded = Encoding.UTF8.GetBytes(serialized);
                    var retryLimit = this.settings.TCPRetryAmount;
                    while (retryLimit > 0)
                    {
                        try
                        {
                            await this.Connect();
                            await this.semaphore.WaitAsync();
                            await this.client.GetStream().WriteAsync(encoded, 0, encoded.Length);
                            await this.client.GetStream().FlushAsync();
                            break;
                        }
                        catch (Exception ex)
                        {
                            this.Disconnect();
                            SelfLog.WriteLine(ex.ToString());
                        }
                        finally
                        {
                            semaphore.Release();
                            retryLimit--;
                        }
                    }
                }
            }
        }

        private void Format(LogEvent logEvent, StringWriter output)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException(nameof(logEvent));
            }
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            output.Write("{\"ts\":\"");
            output.Write(logEvent.Timestamp.UtcDateTime.ToString("O"));
            output.Write("\",\"msgtmpl\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.MessageTemplate.Text, output);

            var tokensWithFormat = logEvent.MessageTemplate.Tokens
                .OfType<PropertyToken>()
                .Where(pt => pt.Format != null);

            // Better not to allocate an array in the 99.9% of cases where this is false
            // ReSharper disable once PossibleMultipleEnumeration
            if (tokensWithFormat.Any())
            {
                output.Write(",\"@r\":[");
                var delim = "";
                foreach (var r in tokensWithFormat)
                {
                    output.Write(delim);
                    delim = ",";
                    var space = new StringWriter();
                    r.Render(logEvent.Properties, space);
                    JsonValueFormatter.WriteQuotedJsonString(space.ToString(), output);
                }
                output.Write(']');
            }

            output.Write(",\"level\":\"");
            output.Write(logEvent.Level);
            output.Write('\"');

            if (logEvent.Exception != null)
            {
                output.Write(",\"ex\":");
                JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
            }

            foreach (var property in logEvent.Properties)
            {
                var name = property.Key;
                if (name.Length > 0 && name[0] == '@')
                {
                    // Escape first '@' by doubling
                    name = '@' + name;
                }

                output.Write(',');
                JsonValueFormatter.WriteQuotedJsonString(name, output);
                output.Write(':');
                new JsonValueFormatter(typeTagName: "$type").Format(property.Value, output);
            }

            output.Write('}');
        }

        private void Disconnect()
        {
            this.client = null;
        }

        protected override void Dispose(bool disposing)
        {
            this.Disconnect();

            base.Dispose(disposing);
        }
    }
}
