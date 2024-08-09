using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Core.Extensions;
using FlowSynx.Logging.InMemory;
using Microsoft.Extensions.DependencyInjection;
using FlowSynx.Logging;
using FlowSynx.Parsers.Date;
using FlowSynx.Parsers.Extensions;

namespace FlowSynx.Core.Features.Logs.Query.List;

internal class LogsListHandler : IRequestHandler<LogsListRequest, Result<IEnumerable<LogsListResponse>>>
{
    private readonly ILogger<LogsListHandler> _logger;
    private readonly IDateParser _dateParser;
    private readonly InMemoryLoggerProvider? _inMemoryLogger;

    public LogsListHandler(ILogger<LogsListHandler> logger, IDateParser dateParser, IServiceProvider serviceProvider)
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

    public async Task<Result<IEnumerable<LogsListResponse>>> Handle(LogsListRequest listRequest, CancellationToken cancellationToken)
    {
        try
        {

            EnsureArg.IsNotNull(_inMemoryLogger, nameof(_inMemoryLogger));

            var predicate = PredicateBuilder.True<LogMessage>();

            if (!string.IsNullOrEmpty(listRequest.MinAge))
            {
                var parsedDateTime = _dateParser.Parse(listRequest.MinAge);
                predicate = predicate.And(p => p.TimeStamp >= parsedDateTime);
            }

            if (!string.IsNullOrEmpty(listRequest.MaxAge))
            {
                var parsedDateTime = _dateParser.Parse(listRequest.MaxAge);
                predicate = predicate.And(p => p.TimeStamp <= parsedDateTime);
            }

            if (!string.IsNullOrEmpty(listRequest.Level))
            {
                var level = listRequest.Level.ToStandardLogLevel();
                predicate = predicate.And(p => p.Level == level);
            }

            var result = _inMemoryLogger.RecordedLogs.Where(predicate.Compile());
            var response = result.Select(x => new LogsListResponse()
            {
                UserName = x.UserName,
                Machine = x.Machine,
                TimeStamp = x.TimeStamp,
                Message = x.Message,
                Level = x.Level.ToFlowSynxLogLevel().ToString().ToUpper(),
            });

            return await Result<IEnumerable<LogsListResponse>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<LogsListResponse>>.FailAsync(new List<string> { ex.Message });
        }
    }
}