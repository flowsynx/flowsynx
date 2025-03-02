using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Logging.ConsoleLogger;

[ProviderAlias("Console")]
public class ConsoleLoggerProvider : ILoggerProvider
{
    private readonly ConsoleLoggerOptions _options;

    public ConsoleLoggerProvider(ConsoleLoggerOptions options)
    {
        _options = options;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new ConsoleLogger(categoryName, _options);
    }

    public void Dispose()
    {

    }
}