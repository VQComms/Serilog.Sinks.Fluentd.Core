using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;
using ServiceStack;
using ServiceStack.Text;

namespace Serilog.fluentd
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
                var formatter = new CompactJsonFormatter();
                formatter.Format(message, sw);

                var serialized = $"[\"{this.Settings.Tag}\",{message.Timestamp.ToUnixTimeSeconds()},{sw.ToString()}]";
                var encoded = serialized.ToUtf8Bytes();

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