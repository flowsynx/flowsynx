using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Logging.ConsoleLogger;

internal class ConsoleLogger : ILogger, IDisposable
{
    private readonly LogQueue _logQueue;
    private readonly string _category;
    private readonly ConsoleLoggerOptions _options;
    private readonly Task _workerTask;

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
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && logLevel >= _options.MinLevel;
    }

    void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        if (formatter == null)
            return;

        var logMessage = new LogMessage
        {
            Level = logLevel,
            EventId = eventId,
            Message = formatter(state, exception),
            TimeStamp = DateTime.UtcNow,
            Category = _category,
            Exception = exception?.ToString(),
        };

        _logQueue.Enqueue(logMessage);
    }

    private async Task ProcessQueue(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_logQueue.TryDequeue(out var logMessage))
            {
                WriteLine(logMessage, cancellationToken);
            }
            else
            {
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    private void WriteLine(LogMessage logMessage, CancellationToken cancellationToken)
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
            throw;
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }

    public void Dispose()
    {
        if (!_options.CancellationToken.IsCancellationRequested)
            _workerTask.Wait();
    }
}