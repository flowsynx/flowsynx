using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Logging.ConsoleLogger;

[ProviderAlias("Console")]
public sealed class ConsoleLoggerProvider(ConsoleLoggerOptions options) : ILoggerProvider
{
    private readonly ConsoleLoggerOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly List<ConsoleLogger> _loggers = new();
    private bool _disposed;
    private readonly object _lock = new();

    public ILogger CreateLogger(string categoryName)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ConsoleLoggerProvider));

        var logger = new ConsoleLogger(categoryName, _options);
        lock (_lock)
        {
            _loggers.Add(logger);
        }
        return logger;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            lock (_lock)
            {
                foreach (var logger in _loggers)
                {
                    logger.Dispose();
                }
                _loggers.Clear();
            }
        }

        _disposed = true;
    }
}