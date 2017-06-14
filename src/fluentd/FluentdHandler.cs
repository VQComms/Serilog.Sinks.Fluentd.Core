using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.Text;

namespace Serilog.fluentd
{
    public class FluentdHandler : IDisposable
    {

        public class FluentdHandlerSettings
        {
            public string Tag { get; set; }
            public string Host { get; set; }
            public int Port { get; set; }
            public int Timeout { get; set; }

        }

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private FluentdHandlerSettings Settings { get; }
        private TcpClient Client { get; set; }
        private static readonly DateTime unixEpochUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private const long _ticksToMilliseconds = 10000;

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

        public async Task Emit(string logMessage, params object[] obj)
        {
            var messageToSend = new FluentdMessage(this.Settings.Tag, FormatDateTime(DateTime.Now), logMessage);
            await this.Send(messageToSend);
        }

        public static long FormatDateTime(DateTime value)
        {
            // Note: microseconds and nanoseconds should always truncated, so deviding by integral is suitable.
            return value.ToUniversalTime().Subtract(unixEpochUtc).Ticks / _ticksToMilliseconds;
        }

        private async Task Send(FluentdMessage message)
        {
            await semaphore.WaitAsync();

            try
            {
                JsConfig.ExcludeTypeInfo = true;
                var serialized = $"[\"{this.Settings.Tag}\",{message.Message.time},{message.Message.record.ToJson()}]";
                var encoded = serialized.ToUtf8Bytes();
                try
                {
                    await Connect();
                    await this.Client.GetStream().WriteAsync(encoded, 0, encoded.Length);
                    Console.WriteLine(System.Text.Encoding.ASCII.GetString(encoded));
                    await this.Client.GetStream().FlushAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Disconnect();
                    throw ex;
                    //TODO: Retry
                }
            }
            finally
            {
                semaphore.Release();
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