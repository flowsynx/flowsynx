using Microsoft.Extensions.Logging;
using System.IO;

namespace FlowSynx.Infrastructure.Logging.ConsoleLogger;

internal class ConsoleLogger : ILogger, IDisposable
{
    private readonly LogQueue _logQueue;
    private readonly string _category;
    private readonly ConsoleLoggerOptions _options;
    private readonly Task _workerTask;
    private readonly Lock _consoleLock = new Lock();
    private static readonly AsyncLocal<Scope?> _currentScope = new();

    private Dictionary<LogLevel, ConsoleColor> ColorMap { get; set; } = new()
    {
        [LogLevel.Trace] = ConsoleColor.DarkMagenta,
        [LogLevel.Debug] = ConsoleColor.DarkCyan,
        [LogLevel.Information] = Console.ForegroundColor,
        [LogLevel.Warning] = ConsoleColor.DarkYellow,
        [LogLevel.Error] = ConsoleColor.DarkRed,
        [LogLevel.Critical] = ConsoleColor.Red
    };

    public ConsoleLogger(string category, ConsoleLoggerOptions options)
    {
        _logQueue = new LogQueue();
        _category = category;
        _options = options;
        _workerTask = Task.Run(() => ProcessQueue(_options.CancellationToken));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        var parent = _currentScope.Value;
        var newScope = new Scope(state, parent);
        _currentScope.Value = newScope;
        return newScope;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        lock (_consoleLock)
        {
            return logLevel != LogLevel.None && logLevel >= _options.MinLevel;
        }
    }

    void ILogger.Log<TState>(
        LogLevel logLevel, 
        EventId eventId, 
        TState state, 
        Exception? exception, 
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var scopeInfo = GetScopeInfo();

        var logMessage = new LogMessage
        {
            Level = logLevel,
            EventId = eventId,
            Message = formatter(state, exception),
            TimeStamp = DateTime.UtcNow,
            Category = _category,
            Exception = exception?.ToString(),
            Scope = scopeInfo
        };

        _logQueue.Enqueue(logMessage);
    }

    private string GetScopeInfo()
    {
        var scopes = new List<string>();
        var current = _currentScope.Value;

        while (current != null)
        {
            if (current.State is IEnumerable<KeyValuePair<string, object>> dict)
            {
                foreach (var item in dict)
                {
                    if (IsFrameworkScopeKey(item.Key))
                        continue;

                    scopes.Add($"{item.Key}={item.Value}");
                }
            }
            else
            {
                scopes.Add(current.State?.ToString() ?? "");
            }
            current = current.Parent;
        }

        return string.Join(" | ", scopes);
    }

    private static bool IsFrameworkScopeKey(string key) => key is "RequestId" or "ConnectionId" or "RequestPath";

    private async Task ProcessQueue(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_logQueue.TryDequeue(out var logMessage))
            {
                WriteLine(logMessage);
            }
            else
            {
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    private void WriteLine(LogMessage logMessage)
    {
        lock (_consoleLock)
        {
            var originalColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = ColorMap[logMessage.Level];
                Console.WriteLine(LogTemplate.Format(logMessage, _options.OutputTemplate));
                Console.ForegroundColor = originalColor;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ColorMap[LogLevel.Error];
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = originalColor;
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
    }

    public void Dispose()
    {
        if (!_options.CancellationToken.IsCancellationRequested)
            _workerTask.Wait();
    }

    private class Scope : IDisposable
    {
        public object? State { get; }
        public Scope? Parent { get; }

        public Scope(object? state, Scope? parent)
        {
            State = state;
            Parent = parent;
        }

        public void Dispose()
        {
            if (_currentScope.Value == this)
            {
                _currentScope.Value = Parent;
            }
        }
    }
}