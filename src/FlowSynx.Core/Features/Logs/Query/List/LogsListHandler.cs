using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.Logging.Extensions;
using FlowSynx.Logging.InMemory;
using Microsoft.Extensions.DependencyInjection;
using FlowSynx.Logging.Filters;
using FlowSynx.Logging.Options;

namespace FlowSynx.Core.Features.Logs.Query.List;

internal class LogsListHandler : IRequestHandler<LogsListRequest, Result<IEnumerable<LogsListResponse>>>
{
    private readonly ILogger<LogsListHandler> _logger;
    private readonly ILogFilter _logFilter;
    private readonly InMemoryLoggerProvider? _inMemoryLogger;

    public LogsListHandler(ILogger<LogsListHandler> logger, ILogFilter logFilter, IServiceProvider serviceProvider)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(logFilter, nameof(logFilter));
        EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
        _logger = logger;
        _logFilter = logFilter;
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

    public async Task<Result<IEnumerable<LogsListResponse>>> Handle(LogsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            EnsureArg.IsNotNull(_inMemoryLogger, nameof(_inMemoryLogger));

            var searchOptions = new LogSearchOptions()
            {
                Include = request.Include,
                Exclude = request.Exclude,
                MinimumAge = request.MinAge,
                MaximumAge = request.MaxAge,
                CaseSensitive = request.CaseSensitive ?? false,
                Level = request.Level,
            };

            var listOptions = new LogListOptions()
            {
                Sorting = request.Sorting,
                MaxResult = request.MaxResults
            };

            var result = _logFilter.FilterLogsList(_inMemoryLogger.RecordedLogs, searchOptions, listOptions);
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