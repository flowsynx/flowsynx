using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Workflow;
using FlowSynx.Application.Localizations;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionList;

internal class WorkflowExecutionListHandler : IRequestHandler<WorkflowExecutionListRequest, 
    Result<IEnumerable<WorkflowExecutionListResponse>>>
{
    private readonly ILogger<WorkflowExecutionListHandler> _logger;
    private readonly IWorkflowExecutionService _workflowExecutionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public WorkflowExecutionListHandler(
        ILogger<WorkflowExecutionListHandler> logger,
        IWorkflowExecutionService workflowExecutionService,
        ICurrentUserService currentUserService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowExecutionService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _workflowExecutionService = workflowExecutionService;
        _currentUserService = currentUserService;
        _localization = localization;
    }

    public async Task<Result<IEnumerable<WorkflowExecutionListResponse>>> Handle(
        WorkflowExecutionListRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var executions = await _workflowExecutionService.All(_currentUserService.UserId, 
                workflowId, cancellationToken);

            var response = executions.Select(execution => new WorkflowExecutionListResponse
            {
                Id = execution.Id,
                Status = execution.Status,
                ExecutionStart = execution.ExecutionStart,
                ExecutionEnd = execution.ExecutionEnd,
            });
            _logger.LogInformation(_localization.Get("Feature_WorkflowExecution_List_RetrievedSuccessfully"));
            return await Result<IEnumerable<WorkflowExecutionListResponse>>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<IEnumerable<WorkflowExecutionListResponse>>.FailAsync(ex.ToString());
        }
    }
}