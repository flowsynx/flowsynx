using FlowSynx.Application.Features.PluginConfig.Query.List;
using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Workflow;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Workflows.Query.List;

internal class WorkflowListHandler : IRequestHandler<WorkflowListRequest, Result<IEnumerable<WorkflowListResponse>>>
{
    private readonly ILogger<PluginConfigListHandler> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISystemClock _systemClock;

    public WorkflowListHandler(ILogger<PluginConfigListHandler> logger,
        IWorkflowService workflowService, ICurrentUserService currentUserService,
        ISystemClock systemClock)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _workflowService = workflowService;
        _currentUserService = currentUserService;
        _systemClock = systemClock;
    }

    public async Task<Result<IEnumerable<WorkflowListResponse>>> Handle(WorkflowListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAthenticationIsRequired, "Access is denied. Authentication is required.");

            var workflows = await _workflowService.All(_currentUserService.UserId, cancellationToken);
            var response = workflows.Select(workflow => new WorkflowListResponse
            {
                Id = workflow.Id,
                Name = workflow.Name,
                ModifiedDate = workflow.LastModifiedOn ?? _systemClock.UtcNow

            });
            _logger.LogInformation("Plugin Config List is got successfully.");
            return await Result<IEnumerable<WorkflowListResponse>>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<IEnumerable<WorkflowListResponse>>.FailAsync(ex.ToString());
        }
    }
}