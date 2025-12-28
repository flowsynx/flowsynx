using FlowSynx.Application.Extensions;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Entities;
using FlowSynx.Domain.Primitives;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.LogEntries.Query.LogEntriesList;

internal class LogEntriesListHandler : IRequestHandler<LogEntriesListRequest, PaginatedResult<LogEntriesListResponse>>
{
    private readonly ILogger<LogEntriesListHandler> _logger;
    private readonly ILogEntryRepository _logEntryRepository;
    private readonly ICurrentUserService _currentUserService;

    public LogEntriesListHandler(ILogger<LogEntriesListHandler> logger, ILogEntryRepository logEntryRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logEntryRepository = logEntryRepository ?? throw new ArgumentNullException(nameof(logEntryRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<PaginatedResult<LogEntriesListResponse>> Handle(LogEntriesListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var predicate = PredicateBuilder.Create<LogEntry>(p => p.UserId == _currentUserService.UserId());

            if (!string.IsNullOrEmpty(request.Level))
                predicate = predicate.And(p => p.Level == ToLogsLevel(request.Level));

            if (request.FromDate != null)
                predicate = predicate.And(p => p.TimeStamp >= request.FromDate);

            if (request.ToDate != null)
                predicate = predicate.And(p => p.TimeStamp <= request.ToDate);

            if (!string.IsNullOrWhiteSpace(request.Message))
                predicate = predicate.And(p =>
                    p.Message != null &&
                    p.Message.Contains(request.Message, StringComparison.OrdinalIgnoreCase));

            var logs = await _logEntryRepository.All(predicate, cancellationToken);
            var response = logs.Select(l => new LogEntriesListResponse
            {
                Id = l.Id,
                Level = l.Level,
                TimeStamp = l.TimeStamp,
                Message = l.Message,
                Exception = l.Exception
            });
            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);
            return await PaginatedResult<LogEntriesListResponse>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex, "FlowSynx exception caught in LogsListHandler.");
            return await PaginatedResult<LogEntriesListResponse>.FailureAsync(ex.Message);
        }
    }

    private static string ToLogsLevel(string logsLevel)
    {
        if (!Enum.TryParse<LogLevel>(logsLevel, ignoreCase: true, out var level))
        {
            return LogLevel.Information.ToString();
        }

        return level.ToString();
    }
}

