using FlowSynx.Core.Features.PluginConfig.Query.List;
using FlowSynx.Core.Services;
using FlowSynx.Core.Wrapper;
using FlowSynx.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Core.Features.Workflows.Query.List;

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
                throw new UnauthorizedAccessException("User is not authenticated.");

            var workflows = await _workflowService.All(_currentUserService.UserId, cancellationToken);
            var response = workflows.Select(workflow => new WorkflowListResponse
            {
                Id = workflow.Id,
                Name = workflow.Name,
                ModifiedDate = workflow.LastModifiedOn ?? _systemClock.NowUtc
                
            });
            _logger.LogInformation("Plugin Config List is got successfully.");
            return await Result<IEnumerable<WorkflowListResponse>>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<IEnumerable<WorkflowListResponse>>.FailAsync(new List<string> { ex.Message });
        }
    }
}