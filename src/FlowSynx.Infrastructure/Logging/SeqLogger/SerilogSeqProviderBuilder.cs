using FlowSynx.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FlowSynx.Infrastructure.Logging.SeqLogger;

public sealed class SerilogSeqProviderBuilder : ILogProviderBuilder
{
    public ILoggerProvider? Build(
        string name, 
        Application.Configuration.System.Logger.LoggerProviderConfiguration? config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var level = config.LogLevel.ToSerilogLevel();

        var serverUrl = config.Url 
            ?? throw new ArgumentNullException(nameof(config.Url), "System:Logger:Providers:Seq:Url cannot be null.");

        var apiKey = config.ApiKey 
            ?? throw new ArgumentNullException(nameof(config.ApiKey), "System:Logger:Providers:Seq:ApiKey cannot be null.");

        return new Serilog.Extensions.Logging.SerilogLoggerProvider(
            new LoggerConfiguration()
                .MinimumLevel.Is(level)
                .WriteTo.Seq(serverUrl: serverUrl, apiKey: apiKey).CreateLogger()
        );
    }
}