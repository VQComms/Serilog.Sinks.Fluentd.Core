# Serilog.Sinks.Fluentd.Core

Sends your logs to [Fluentd]()

```csharp
var log = new LoggerConfiguration()
                .WriteTo.Fluentd()
                .CreateLogger();
```
