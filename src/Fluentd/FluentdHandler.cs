using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Parsing;

namespace Serilog.Sinks.Fluentd.Core.Fluentd
{
    public class FluentdHandler : IDisposable
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private FluentdHandlerSettings Settings { get; }
        private TcpClient Client { get; set; }

        private FluentdHandler(FluentdHandlerSettings settings)
        {
            this.Settings = settings;
        }

        public static async Task<FluentdHandler> CreateHandler(string tag, FluentdHandlerSettings settings)
        {
            var handler = new FluentdHandler(settings);
            await handler.Connect();
            return handler;
        }

        private async Task Connect()
        {
            if (this.Client == null)
            {
                this.Client = new TcpClient();
                this.Client.SendTimeout = this.Settings.Timeout;
                await this.Client.ConnectAsync(this.Settings.Host, this.Settings.Port);
            }
        }

        public async Task Send(LogEvent message)
        {
            using (var sw = new StringWriter())
            {
                this.Format(message, sw);

                var serialized = $"[\"{this.Settings.Tag}\",{message.Timestamp.ToUnixTimeSeconds()},{sw.ToString()}]";
                var encoded = Encoding.UTF8.GetBytes(serialized);

                try
                {
                    await semaphore.WaitAsync();
                    await Connect();
                    await this.Client.GetStream().WriteAsync(encoded, 0, encoded.Length);
                    await this.Client.GetStream().FlushAsync();
                }
                catch (Exception ex)
                {
                    Disconnect();
                    throw ex;
                    //TODO: Retry
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }

        private void Format(LogEvent logEvent, StringWriter output)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
            if (output == null) throw new ArgumentNullException(nameof(output));

            output.Write("{\"@t\":\"");
            output.Write(logEvent.Timestamp.UtcDateTime.ToString("O"));
            output.Write("\",\"@mt\":");
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

            output.Write(",\"@l\":\"");
            output.Write(logEvent.Level);
            output.Write('\"');


            if (logEvent.Exception != null)
            {
                output.Write(",\"@x\":");
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
            this.Client = null;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}