using FlowSynx.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FlowSynx.Infrastructure.Logging.ConsoleLogger;

public sealed class SerilogConsoleProviderBuilder : ILogProviderBuilder
{
    public ILoggerProvider? Build(
        string name, 
        Application.Configuration.System.Logger.LoggerProviderConfiguration config)
    {
        var level = config.LogLevel.ToSerilogLevel();

        return new Serilog.Extensions.Logging.SerilogLoggerProvider(
            new LoggerConfiguration()
                .MinimumLevel.Is(level)
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger()
        );
    }
}