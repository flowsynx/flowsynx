using FlowSynx.Application.Extensions;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Wrapper;
using FlowSynx.Domain.Log;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Logs.Query.LogsList;

internal class LogsListHandler : IRequestHandler<LogsListRequest, PaginatedResult<LogsListResponse>>
{
    private readonly ILogger<LogsListHandler> _logger;
    private readonly ILoggerService _loggerService;
    private readonly ICurrentUserService _currentUserService;

    public LogsListHandler(ILogger<LogsListHandler> logger, ILoggerService loggerService,
        ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(loggerService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _loggerService = loggerService;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedResult<LogsListResponse>> Handle(LogsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var predicate = PredicateBuilder.Create<LogEntity>(p => p.UserId == _currentUserService.UserId());

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

            var logs = await _loggerService.All(predicate, cancellationToken);
            var response = logs.Select(l => new LogsListResponse
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
            return await PaginatedResult<LogsListResponse>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex, "FlowSynx exception caught in LogsListHandler.");
            return await PaginatedResult<LogsListResponse>.FailureAsync(ex.Message);
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

