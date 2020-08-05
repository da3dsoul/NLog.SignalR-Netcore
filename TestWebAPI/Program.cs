using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.SignalR.Netcore.NLog;
using NLog.Targets.Wrappers;
using NLog.Web;
using LogLevel = NLog.LogLevel;

namespace TestWebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            InitLogger();
            CreateHostBuilder(args).Build().Run();
        }

        private static void InitLogger()
        {
            var config = new LoggingConfiguration();
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logconsole);
            LogManager.Configuration = config;
            // Create SignalRTarget. Trim the layout to only give the message, as the other details are provided in LogEvent
            var signalRTarget = new SignalRTarget { ConnectMethodName = "OnConnect", LogMethodName = "Log", Layout = "${message}" };
            // Make an Async Wrapper for the Target, to prevent blocking the logger
            var asyncTarget = new AsyncTargetWrapper(signalRTarget, 50, AsyncTargetWrapperOverflowAction.Discard);
            LogManager.Configuration.AddTarget("signalr", asyncTarget);
            LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, asyncTarget));
            LogManager.ReconfigExistingLoggers();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                }).UseNLog();
    }
}