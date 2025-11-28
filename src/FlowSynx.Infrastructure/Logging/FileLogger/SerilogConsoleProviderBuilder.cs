using FlowSynx.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FlowSynx.Infrastructure.Logging.FileLogger;

public sealed class SerilogFileProviderBuilder : ILogProviderBuilder
{
    public ILoggerProvider? Build(
        string name, 
        Application.Configuration.System.Logger.LoggerProviderConfiguration config)
    {
        var level = config.LogLevel.ToSerilogLevel();

        return new Serilog.Extensions.Logging.SerilogLoggerProvider(
            new LoggerConfiguration()
                .MinimumLevel.Is(level)
                .WriteTo.File(
                    path: config.FilePath!,
                    rollingInterval: config.RollingInterval.RollingIntervalFromString(),
                    retainedFileCountLimit: config.RetainedFileCountLimit,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [Thread:{ThreadId}] [Machine:{MachineName}] [Process:{ProcessName}:{ProcessId}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
                ).CreateLogger()
        );
    }
}