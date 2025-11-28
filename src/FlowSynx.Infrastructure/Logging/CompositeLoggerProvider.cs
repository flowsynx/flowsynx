using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Logging;

public class CompositeLoggerProvider : ILoggerProvider
{
    private readonly IEnumerable<ILoggerProvider> _providers;

    public CompositeLoggerProvider(IEnumerable<ILoggerProvider> providers)
    {
        _providers = providers;
    }

    public ILogger CreateLogger(string categoryName)
    {
        var loggers = _providers.Select(p => p.CreateLogger(categoryName)).ToList();
        return new CompositeLogger(loggers);
    }

    public void Dispose()
    {
        foreach (var provider in _providers)
            provider.Dispose();
    }
}

public class CompositeLogger : ILogger
{
    private readonly IEnumerable<ILogger> _loggers;
    private static readonly AsyncLocal<Scope?> _currentScope = new();

    public CompositeLogger(IEnumerable<ILogger> loggers)
    {
        _loggers = loggers;
    }


    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        var parent = _currentScope.Value;
        var newScope = new Scope(state, parent);
        _currentScope.Value = newScope;
        return newScope;
    }

    public bool IsEnabled(LogLevel logLevel) =>
        _loggers.Any(l => l.IsEnabled(logLevel));

    public void Log<TState>(LogLevel logLevel, EventId eventId,
                            TState state, Exception? exception,
                            Func<TState, Exception?, string> formatter)
    {
        foreach (var logger in _loggers)
        {
            if (logger.IsEnabled(logLevel))
                logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }

    private sealed class Scope(object? state, Scope? parent) : IDisposable
    {
        private bool _disposed;

        public object? State { get; } = state;
        public Scope? Parent { get; } = parent;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            // Only clear the current scope if this instance is the active one
            if (disposing && _currentScope.Value == this)
            {
                _currentScope.Value = Parent;
            }

            _disposed = true;
        }
    }
}