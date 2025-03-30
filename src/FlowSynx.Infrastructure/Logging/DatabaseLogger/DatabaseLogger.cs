using FlowSynx.Application.Models;
using FlowSynx.Domain.Entities.Log;
using FlowSynx.Domain.Interfaces;
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

    public DatabaseLogger(string category, DatabaseLoggerOptions options,
        IHttpContextAccessor httpContextAccessor, ILoggerService loggerService)
    {
        _logQueue = new LogQueue();
        _category = category;
        _options = options;
        _httpContextAccessor = httpContextAccessor;
        _loggerService = loggerService;
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

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        if (formatter == null)
            return;

        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

        var logMessage = new LogMessage
        {
            UserId = userId,
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
}