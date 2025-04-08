using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.Application.Extensions;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Domain.Log;

namespace FlowSynx.Application.Features.Logs.Query.List;

internal class LogsListHandler : IRequestHandler<LogsListRequest, Result<IEnumerable<LogsListResponse>>>
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

    public async Task<Result<IEnumerable<LogsListResponse>>> Handle(LogsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var predicate = PredicateBuilder.Create<LogEntity>(p => p.UserId == _currentUserService.UserId);

            if (!string.IsNullOrEmpty(request.Level))
                predicate = predicate.And(p => p.Level == ToLogsLevel(request.Level));

            if (request.FromDate != null)
                predicate = predicate.And(p => p.TimeStamp >= request.FromDate);

            if (request.ToDate != null)
                predicate = predicate.And(p => p.TimeStamp <= request.ToDate);

            if (!string.IsNullOrEmpty(request.Message))
                predicate = predicate.And(p => p.Message.ToLower().Contains(request.Message.ToLower()));

            var logs = await _loggerService.All(predicate, cancellationToken);
            var response = logs.Select(l => new LogsListResponse
            {
                Id = l.Id,
                Level = l.Level,
                TimeStamp = l.TimeStamp,
                Message = l.Message,
                Exception = l.Exception
            });
            return await Result<IEnumerable<LogsListResponse>>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<IEnumerable<LogsListResponse>>.FailAsync(ex.ToString());
        }
    }

    private LogsLevel ToLogsLevel(string logsLevel)
    {
        var level = logsLevel.ToLower() switch
        {
            "none" => LogsLevel.None,
            "dbug" => LogsLevel.Dbug,
            "info" => LogsLevel.Info,
            "warn" => LogsLevel.Warn,
            "fail" => LogsLevel.Fail,
            "crit" => LogsLevel.Crit,
            _ => LogsLevel.Info,
        };

        return level;
    }
}