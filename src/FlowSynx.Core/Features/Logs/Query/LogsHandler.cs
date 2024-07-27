using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Extensions;
using FlowSynx.Logging.InMemory;
using Microsoft.Extensions.DependencyInjection;
using FlowSynx.Logging;
using FlowSynx.Parsers.Date;
using FlowSynx.Commons;

namespace FlowSynx.Core.Features.Logs.Query;

internal class LogsHandler : IRequestHandler<LogsRequest, Result<IEnumerable<LogsResponse>>>
{
    private readonly ILogger<LogsHandler> _logger;
    private readonly IDateParser _dateParser;
    private readonly InMemoryLoggerProvider? _inMemoryLogger;

    public LogsHandler(ILogger<LogsHandler> logger, IDateParser dateParser, IServiceProvider serviceProvider)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
        EnsureArg.IsNotNull(dateParser, nameof(dateParser));
        _logger = logger;
        _dateParser = dateParser;
        var loggerProviders = serviceProvider.GetServices<ILoggerProvider>();
        _inMemoryLogger = GeInMemoryLoggerProvider(loggerProviders);
    }

    private InMemoryLoggerProvider? GeInMemoryLoggerProvider(IEnumerable<ILoggerProvider> providers)
    {
        foreach (var provider in providers)
        {
            if (provider is InMemoryLoggerProvider loggerProvider)
            {
                return loggerProvider;
            }
        }

        return null;
    }

    public async Task<Result<IEnumerable<LogsResponse>>> Handle(LogsRequest request, CancellationToken cancellationToken)
    {
        try
        {

            EnsureArg.IsNotNull(_inMemoryLogger, nameof(_inMemoryLogger));
            
            var predicate = PredicateBuilder.True<LogMessage>();

            if (!string.IsNullOrEmpty(request.MinAge))
            {
                var parsedDateTime = _dateParser.Parse(request.MinAge);
                predicate = predicate.And(p => p.TimeStamp >= parsedDateTime);
            }

            if (!string.IsNullOrEmpty(request.MaxAge))
            {
                var parsedDateTime = _dateParser.Parse(request.MaxAge);
                predicate = predicate.And(p => p.TimeStamp <= parsedDateTime);
            }

            if (!string.IsNullOrEmpty(request.Level))
            {
                var level = GetLogLevel(request.Level);
                predicate = predicate.And(p => p.Level == level);
            }

            var result = _inMemoryLogger.RecordedLogs.Where(predicate.Compile());
            var response = result.Select(x => new LogsResponse()
            {
                UserName = x.UserName,
                Machine = x.Machine,
                TimeStamp = x.TimeStamp,
                Message = x.Message,
                Level = x.Level,
            });

            return await Result<IEnumerable<LogsResponse>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<LogsResponse>>.FailAsync(new List<string> { ex.Message });
        }
    }

    private LogLevel GetLogLevel(string logLevel)
    {
        var loggingLevel = string.IsNullOrEmpty(logLevel)
            ? LoggingLevel.Info
            : EnumUtils.GetEnumValueOrDefault<LoggingLevel>(logLevel)!.Value;

        var level = loggingLevel switch
        {
            LoggingLevel.Dbug => LogLevel.Debug,
            LoggingLevel.Info => LogLevel.Information,
            LoggingLevel.Warn => LogLevel.Warning,
            LoggingLevel.Fail => LogLevel.Error,
            LoggingLevel.Crit => LogLevel.Critical,
            _ => LogLevel.Information,
        };

        return level;
    }
}