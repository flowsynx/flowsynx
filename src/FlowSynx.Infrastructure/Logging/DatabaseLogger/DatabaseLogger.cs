using FlowSynx.Application.Models;
using FlowSynx.Domain.Log;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace FlowSynx.Infrastructure.Logging.DatabaseLogger;

internal class DatabaseLogger : ILogger, IDisposable
{
    private readonly LogQueue _logQueue;
    private readonly string _category;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILoggerService _loggerService;
    private readonly DatabaseLoggerOptions _options;
    private readonly Task _workerTask;
    private static readonly AsyncLocal<Scope?> _currentScope = new();

    public DatabaseLogger(string category, DatabaseLoggerOptions options,
        IHttpContextAccessor httpContextAccessor, ILoggerService loggerService)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(loggerService);
        _logQueue = new LogQueue();
        _category = category;
        _options = options;
        _httpContextAccessor = httpContextAccessor;
        _loggerService = loggerService;
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
        return logLevel != LogLevel.None && logLevel >= _options.MinLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

        var scopeInfo = GetScopeInfo();

        var logMessage = new LogMessage
        {
            UserId = userId,
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
                await WriteLine(logMessage, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    private async Task WriteLine(LogMessage logMessage, CancellationToken cancellationToken)
    {
        try
        {
            var log = new LogEntity
            {
                Id = Guid.NewGuid(),
                UserId = logMessage.UserId,
                Level = ToLogsLevel(logMessage.Level),
                TimeStamp = logMessage.TimeStamp,
                Message = logMessage.Message,
                Category = logMessage.Category,
                Exception = logMessage.Exception,
                Scope = logMessage.Scope,
            };

            await _loggerService.Add(log, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new FlowSynxException((int)ErrorCode.LogAdd, ex.Message);
        }
    }

    private LogsLevel ToLogsLevel(LogLevel logLevel)
    {
        var level = logLevel switch
        {
            LogLevel.Debug => LogsLevel.Dbug,
            LogLevel.Information => LogsLevel.Info,
            LogLevel.Warning => LogsLevel.Warn,
            LogLevel.Error => LogsLevel.Fail,
            LogLevel.Critical => LogsLevel.Crit,
            _ => LogsLevel.Info,
        };

        return level;
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