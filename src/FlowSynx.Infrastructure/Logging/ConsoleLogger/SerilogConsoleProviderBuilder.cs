//using FlowSynx.Infrastructure.Extensions;
//using Microsoft.Extensions.Logging;
//using Serilog;
//using SerilogLoggerConfiguration = Serilog.LoggerConfiguration;

//namespace FlowSynx.Infrastructure.Logging.ConsoleLogger;

//public sealed class SerilogConsoleProviderBuilder : ILogProviderBuilder
//{
//    public ILoggerProvider? Build(
//        string name, 
//        LoggerProviderConfiguration? config)
//    {
//        var level = config?.LogLevel.ToSerilogLevel() ?? Serilog.Events.LogEventLevel.Information;
//        var outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{TenantId}] [{SourceContext}] " +
//            "{Message:lj}{NewLine}{Exception}";

//        return new Serilog.Extensions.Logging.SerilogLoggerProvider(
//            new SerilogLoggerConfiguration()
//                .Enrich.FromLogContext()
//                .MinimumLevel.Is(level)
//                .WriteTo.Console(outputTemplate: outputTemplate)
//                .CreateLogger()
//        );
//    }
//}