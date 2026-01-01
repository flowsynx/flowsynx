using FlowSynx.Infrastructure.Configuration.System.Logger;
using FlowSynx.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using SerilogLoggerConfiguration = Serilog.LoggerConfiguration;

namespace FlowSynx.Infrastructure.Logging.FileLogger;

public sealed class SerilogFileProviderBuilder : ILogProviderBuilder
{
    public ILoggerProvider? Build(
        string name, 
        LoggerProviderConfiguration? config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var level = config.LogLevel.ToSerilogLevel();

        var filePath = config.FilePath 
            ?? throw new ArgumentNullException(nameof(config), "System:Logger:Providers:File:FilePath cannot be null.");

        var outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [Tenant:{TenantId}] [Thread:{ThreadId}] " +
            "[Machine:{MachineName}] [Process:{ProcessName}:{ProcessId}] [{SourceContext}] " +
            "{Message:lj}{NewLine}{Exception}";

        return new Serilog.Extensions.Logging.SerilogLoggerProvider(
            new SerilogLoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Is(level)
                .WriteTo.File(
                    path: filePath,
                    rollingInterval: config.RollingInterval.RollingIntervalFromString(),
                    retainedFileCountLimit: config.RetainedFileCountLimit ?? 7,
                    outputTemplate: outputTemplate
                ).CreateLogger()
        );
    }
}