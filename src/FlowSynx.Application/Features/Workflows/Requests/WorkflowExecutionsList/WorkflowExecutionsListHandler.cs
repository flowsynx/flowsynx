using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Workflows.Requests.WorkflowExecutionsList;

internal class WorkflowExecutionsListHandler 
    : IActionHandler<WorkflowExecutionsListRequest, PaginatedResult<WorkflowExecutionsListResult>>
{
    private readonly ILogger<WorkflowExecutionsListHandler> _logger;
    private readonly IWorkflowApplicationExecutionService _workflowExecutionService;
    private readonly ICurrentUserService _currentUserService;

    public WorkflowExecutionsListHandler(
        ILogger<WorkflowExecutionsListHandler> logger, 
        IWorkflowApplicationExecutionService workflowExecutionService,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workflowExecutionService = workflowExecutionService ?? throw new ArgumentNullException(nameof(workflowExecutionService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<PaginatedResult<WorkflowExecutionsListResult>> Handle(WorkflowExecutionsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var histories = await _workflowExecutionService.GetExecutionHistoryAsync(
                "workflow",
                request.WorkflowId,
                cancellationToken);

            var response = histories.Select(history => new WorkflowExecutionsListResult
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
                Logs = history.Logs.Select(l=>new WorkflowExecutionsLog
                {
                    Timestamp = l.Timestamp,
                    Level = l.Level,
                    Source = l.Source,
                    Message = l.Message,
                    Data = l.Data
                }).ToList(),
                Artifacts = history.Artifacts.Select(a=>new WorkflowExecutionsArtifact
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
                "Workflow execution history list retrieved successfully for page {Page} with size {PageSize}.",
                page,
                pageSize);

            return await PaginatedResult<WorkflowExecutionsListResult>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in WorkflowExecutionsListHandler for page {Page} with size {PageSize}.",
                request.Page,
                request.PageSize);
            return await PaginatedResult<WorkflowExecutionsListResult>.FailureAsync(ex.Message);
        }
    }
}
