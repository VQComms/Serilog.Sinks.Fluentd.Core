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
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        private readonly FluentdHandlerSettings settings;

        private bool connected;

        private TcpClient client;

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
            this.connected = true;
        }

        private async Task Reconnect()
        {
            try
            {
                this.Disconnect();
                await this.Connect();
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine(ex.ToString());
            }
        }

        private void Disconnect()
        {
            this.client?.Dispose();
            this.client = null;
            this.connected = false;
        }

        private async Task Send(IEnumerable<LogEvent> messages)
        {
            if (!this.connected)
            {
                await this.Connect();
            }

            foreach (var logEvent in messages)
            {
                using (var sw = new StringWriter())
                {
                    try
                    {
                        this.Format(logEvent, sw);
                    }
                    catch (Exception ex)
                    {
                        SelfLog.WriteLine(ex.ToString());
                        continue;
                    }

                    var serialized = $"[\"{this.settings.Tag}\",{logEvent.Timestamp.ToUnixTimeSeconds()},{sw}]";
                    var encoded = Encoding.UTF8.GetBytes(serialized);
                    var retryLimit = this.settings.TCPRetryAmount;

                    while (retryLimit > 0)
                    {
                        try
                        {
                            await this.semaphore.WaitAsync();
                            await this.client.GetStream().WriteAsync(encoded, 0, encoded.Length);
                            await this.client.GetStream().FlushAsync();
                            break;
                        }
                        catch (Exception ex)
                        {
                            await this.Reconnect();

                            SelfLog.WriteLine(ex.ToString());
                        }
                        finally
                        {
                            this.semaphore.Release();
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
                foreach (var token in tokensWithFormat)
                {
                    output.Write(delim);
                    delim = ",";
                    var space = new StringWriter();
                    token.Render(logEvent.Properties, space);
                    JsonValueFormatter.WriteQuotedJsonString(space.ToString(), output);
                }
                output.Write(']');
            }

            output.Write(",\"level\":\"");
            output.Write(logEvent.Level);
            output.Write('\"');

            if (logEvent.Exception != null)
            {
                output.Write(',');
                this.WriteException(logEvent.Exception, output);
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
                new JsonValueFormatter("$type").Format(property.Value, output);
            }

            output.Write('}');
        }

        private void WriteException(Exception exception, TextWriter output)
        {
            output.Write("\"");
            output.Write("exceptions");
            output.Write("\":[");

            this.WriteExceptionSerializationInfo(exception, output, 0);
            output.Write("]");
        }

        private void WriteExceptionSerializationInfo(Exception exception, TextWriter output, int depth)
        {
            if (depth > 0)
            {
                output.Write(",");
            }
            output.Write("{");
            WriteSingleException(exception, output, depth);
            output.Write("}");

            if (exception.InnerException != null && depth < 20)
            {
                this.WriteExceptionSerializationInfo(exception.InnerException, output, ++depth);
            }
        }

        private static void WriteSingleException(Exception exception, TextWriter output, int depth)
        {
            var helpUrl = exception.HelpLink;
            var stackTrace = exception.StackTrace ?? "";
            var hresult = exception.HResult;
            var source = exception.Source;

            WriteJsonProperty("Depth", depth, ",", output);
            WriteJsonProperty("Message", exception.Message, ",", output);
            WriteJsonProperty("Source", source, ",", output);

            output.Write("\"StackTraceString\":");
            JsonValueFormatter.WriteQuotedJsonString(stackTrace, output);
            output.Write(",");
            WriteJsonProperty("HResult", hresult, ",", output);
            WriteJsonProperty("HelpURL", helpUrl, "", output);
        }

        private static void WriteJsonProperty(string propName, object value, string delim, TextWriter output)
        {
            output.Write("\"" + propName + "\":\"" + value + "\"" + delim);
        }

        protected override void Dispose(bool disposing)
        {
            this.Disconnect();

            base.Dispose(disposing);
        }
    }
}
