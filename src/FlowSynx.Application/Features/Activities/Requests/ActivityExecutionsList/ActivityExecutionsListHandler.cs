using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Activities.Requests.ActivityExecutionsList;

internal class ActivityExecutionsListHandler : IActionHandler<ActivityExecutionsListRequest, PaginatedResult<ActivityExecutionsListResult>>
{
    private readonly ILogger<ActivityExecutionsListHandler> _logger;
    private readonly IWorkflowApplicationExecutionService _workflowApplicationExecutionService;
    private readonly ICurrentUserService _currentUserService;

    public ActivityExecutionsListHandler(
        ILogger<ActivityExecutionsListHandler> logger, 
        IWorkflowApplicationExecutionService workflowApplicationExecutionService,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workflowApplicationExecutionService = workflowApplicationExecutionService ?? throw new ArgumentNullException(nameof(workflowApplicationExecutionService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<PaginatedResult<ActivityExecutionsListResult>> Handle(ActivityExecutionsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var histories = await _workflowApplicationExecutionService.GetExecutionHistoryAsync(
                "activity",
                request.ActivityId,
                cancellationToken);

            var response = histories.Select(history => new ActivityExecutionsListResult
            {
                ExecutionId = history.ExecutionId,
                TargetType = history.TargetType,
                TargetId = history.TargetId,
                TargetName = history.TargetName,
                Namespace = history.Namespace,
                Request = history.Request,
                Response = history.Response,
                Context = history.Context,
                Parameters = history.Parameters,
                Metadata = history.Metadata,
                Status = history.Status,
                Progress = history.Progress,
                ErrorMessage = history.ErrorMessage,
                ErrorCode = history.ErrorCode,
                StartedAt = history.StartedAt,
                CompletedAt = history.CompletedAt,
                DurationMilliseconds = history.DurationMilliseconds,
                TriggeredBy = history.TriggeredBy,
                Logs = history.Logs.Select(l=>new ActivityExecutionsLog
                {
                    Timestamp = l.Timestamp,
                    Level = l.Level,
                    Source = l.Source,
                    Message = l.Message,
                    Data = l.Data
                }).ToList(),
                Artifacts = history.Artifacts.Select(a=>new ActivityExecutionsArtifact
                {
                    Name = a.Name,
                    Type = a.Type,
                    Content = a.Content,
                    Size = a.Size,
                    CreatedAt = a.CreatedAt
                }).ToList()
            }).ToList();

            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);

            _logger.LogInformation(
                "Activity execution history list retrieved successfully for page {Page} with size {PageSize}.",
                page,
                pageSize);

            return await PaginatedResult<ActivityExecutionsListResult>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in ActivityExecutionsListHandler for page {Page} with size {PageSize}.",
                request.Page,
                request.PageSize);
            return await PaginatedResult<ActivityExecutionsListResult>.FailureAsync(ex.Message);
        }
    }
}
