using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Logging.ConsoleLogger;

[ProviderAlias("Console")]
public class ConsoleLoggerProvider(ConsoleLoggerOptions options) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new ConsoleLogger(categoryName, options);
    }

    public void Dispose()
    {

    }
}