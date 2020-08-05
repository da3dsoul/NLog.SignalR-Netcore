# NLog.SignalR-Netcore
There are a few SignalR Targets for NLog in existence already, but they are old, and they don't fit in well with the newer netcore and SignalR paradigms. This package aims to improve upon that.

## Advantages
- This package is significantly easier to spin up and go than the alternatives with aspnetcore
- Configurable via config and code-first as any NLog Target would be
- Support for custom message Layouts
- The LogEvent model allows a client to access the timestamp, caller, Logger Name, and LogLevel without parsing the message 
- Backlog of configurable length is sent on client connection
- Supports Async without any extra effort, and never blocks the Logger

## Limitations
- This cannot support multiple SignalR Targets simultaneously without overriding some code. This is due to the limitation of SignalR's handling of dependency injection and MapHub<>()
- The Target is accessed statically, as SignalR's dependency inject doesn't allow giving parameters in the constructor. This makes Dependency Injection more complicated and the whole system less generic
- This doesn't support streaming the history piece by piece on connection. The backlog is kept as a Memory Target style implementation, and the client must store the messages if they are to be kept.

## How-to
The sample project gives an example, as well, complete with code-first setup of NLog and SignalR.

*In Startup.cs*
```
void ConfigureServices(IServiceCollection services)
{
...
// Add SignalR
services.AddSignalR();
// Add LoggingEmitter to the pipeline for dependency injection. Theoretically, you can extend it for more complex logic
services.AddSingleton<LoggingEmitter>();
// part of aspnetcore
services.AddControllers();
}

void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
...
app.UseEndpoints(endpoints =>
{
    // part of aspnetcore
    endpoints.MapControllers();
    // We map the LoggingHub or child of it to a path, in this case, "/signalr/logging
    endpoints.MapHub<LoggingHub>("/signalr/logging");
});
}
```

### Code-First
*Somewhere early in the program's init*
```
// Create SignalRTarget. Trim the layout to only give the message, as the other details are provided in LogEvent
var signalRTarget = new SignalRTarget { ConnectMethodName = "OnConnect", LogMethodName = "Log", Layout = "${message}" };
// Make an Async Wrapper for the Target, to prevent blocking the logger
var asyncTarget = new AsyncTargetWrapper(signalRTarget, 50, AsyncTargetWrapperOverflowAction.Discard);
LogManager.Configuration.AddTarget("signalr", asyncTarget);
LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, asyncTarget));
LogManager.ReconfigExistingLoggers();
```

### Config
*In the NLog.config*
```
<targets async="true">
    <target xsi:type="SignalR" Name="signalr" LogMethodName="Log" ConnectMethodName="OnConnect" layout="${message}" />
</targets>

<rules>
  <logger name="*" minlevel="Trace" writeTo="signalr" />
</rules>
```