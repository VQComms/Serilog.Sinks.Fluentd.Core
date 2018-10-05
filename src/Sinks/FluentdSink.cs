namespace Serilog.Sinks.Fluentd.Core.Sinks
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using MsgPack;
    using MsgPack.Serialization;
    using Serilog.Debugging;
    using Serilog.Events;
    using Serilog.Sinks.PeriodicBatching;

    public class FluentdSink : PeriodicBatchingSink
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        private readonly FluentdHandlerSettings settings;

        private readonly SerializationContext serializationContext;

        private readonly SerilogVisitor visitor;

        private TcpClient client;

        public FluentdSink(FluentdHandlerSettings settings) : base(settings.BatchPostingLimit, settings.BatchingPeriod)
        {
            this.settings = settings;
            this.serializationContext = new SerializationContext(PackerCompatibilityOptions.PackBinaryAsRaw) { SerializationMethod = SerializationMethod.Map };
            this.visitor = new SerilogVisitor();
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            await this.Send(events);
        }

        private async Task Send(IEnumerable<LogEvent> messages)
        {
            foreach (var logEvent in messages)
            {
                using (var sw = new MemoryStream())
                {
                    try
                    {
                        var packer = Packer.Create(sw);
                        await packer.PackArrayHeaderAsync(3); //3 fields to store
                        await packer.PackStringAsync(this.settings.Tag, Encoding.UTF8);
                        await packer.PackAsync((ulong)logEvent.Timestamp.ToUnixTimeSeconds());

                        this.FormatwithMsgPack(logEvent, packer);
                    }
                    catch (Exception ex)
                    {
                        SelfLog.WriteLine(ex.ToString());
                        continue;
                    }

                    var retryLimit = this.settings.TCPRetryAmount;

                    while (retryLimit > 0)
                    {
                        try
                        {
                            await this.Connect();
                            await this.semaphore.WaitAsync();
                            var stream = this.client.GetStream();
                            var data = sw.ToArray();
                            await stream.WriteAsync(data, 0, data.Length);
                            await stream.FlushAsync();
                            break;
                        }
                        catch (Exception ex)
                        {
                            this.Disconnect();
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

        private async Task Connect()
        {
            if (this.client != null)
            {
                if (!this.client.Connected)
                {
                    this.client.Dispose();
                    this.client = null;
                }
                else
                {
                    return;
                }
            }

            this.client = new TcpClient
            {
                SendTimeout = this.settings.TCPSendTimeout,
                LingerState = new LingerOption(true, this.settings.LingerTime)
            };

            await this.client.ConnectAsync(this.settings.Host, this.settings.Port);
        }

        private void FormatwithMsgPack(LogEvent logEvent, Packer packer)
        {
            dynamic localEvent = new ExpandoObject();
            localEvent.timeStamp = logEvent.Timestamp.UtcDateTime.ToString("O");
            localEvent.ticks = logEvent.Timestamp.UtcTicks;
            localEvent.msgTmpl = logEvent.MessageTemplate.Text;
            localEvent.msg = logEvent.RenderMessage();
            localEvent.level = logEvent.Level.ToString();

            if (logEvent.Exception != null)
            {
                localEvent.exceptions = new List<LocalException>();
                localEvent.exceptions.Add(new LocalException());
                WriteMsgPackException(logEvent.Exception, localEvent);
            }

            foreach (var property in logEvent.Properties)
            {
                var name = property.Key;
                if (name.Length > 0 && name[0] == '@')
                {
                    // Escape first '@' by doubling
                    name = '@' + name;
                }

                this.visitor.Visit((IDictionary<string, object>)localEvent, name, property.Value);
            }

            packer.Pack((IDictionary<string, object>)localEvent, this.serializationContext);
        }

        /// <summary>
        /// Writes out the attached exception
        /// </summary>
        private void WriteMsgPackException(Exception exception, dynamic localLogEvent)
        {
            this.WriteMsgPackExceptionSerializationInfo(exception, 0, localLogEvent);
        }

        private void WriteMsgPackExceptionSerializationInfo(Exception exception, int depth, dynamic localLogEvent)
        {
            if (depth > 0)
            {
                localLogEvent.exceptions.Add(new LocalException());
            }

            this.WriteMsgPackSingleException(exception, depth, localLogEvent.exceptions[depth]);

            if (exception.InnerException != null && depth < 20)
            {
                this.WriteMsgPackExceptionSerializationInfo(exception.InnerException, ++depth, localLogEvent);
            }
        }

        private void WriteMsgPackSingleException(Exception exception, int depth, dynamic localException)
        {
            var helpUrl = exception.HelpLink;
            var stackTrace = exception.StackTrace ?? "";
            var hresult = exception.HResult;
            var source = exception.Source;

            localException.Depth = depth;
            localException.Message = exception.Message;
            localException.Source = source;
            localException.StackTraceString = stackTrace;
            localException.HResult = hresult;
            localException.HelpURL = helpUrl;
        }

        private void Disconnect()
        {
            this.client?.Dispose();
            this.client = null;
        }

        protected override void Dispose(bool disposing)
        {
            this.Disconnect();

            base.Dispose(disposing);
        }
    }
}
