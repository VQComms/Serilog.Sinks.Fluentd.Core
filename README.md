# Serilog.Sinks.Fluentd.Core

Sends your logs to [Fluentd](https://www.fluentd.org/)

```csharp
var log = new LoggerConfiguration()
                .WriteTo.Fluentd()
                .CreateLogger();
```
