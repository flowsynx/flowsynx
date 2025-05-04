using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Workflow;

namespace FlowSynx.Application.Features.Workflows.Query.Details;

internal class WorkflowDetailsHandler : IRequestHandler<WorkflowDetailsRequest, Result<WorkflowDetailsResponse>>
{
    private readonly ILogger<WorkflowDetailsHandler> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly ICurrentUserService _currentUserService;

    public WorkflowDetailsHandler(
        ILogger<WorkflowDetailsHandler> logger,
        IWorkflowService workflowService, 
        ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _workflowService = workflowService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<WorkflowDetailsResponse>> Handle(WorkflowDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAuthenticationIsRequired, Resources.Authentication_Access_Denied);

            var workflowId = Guid.Parse(request.Id);
            var workflow = await _workflowService.Get(_currentUserService.UserId, workflowId, cancellationToken);
            if (workflow is null)
            {
                var message = string.Format(Resources.Feature_Workflow_Details_WorkflowNotFound, request.Id);
                throw new FlowSynxException((int)ErrorCode.WorkflowNotFound, message);
            }

            var response = new WorkflowDetailsResponse
            {
                Id = workflow.Id,
                Name = workflow.Name,
                Workflow = workflow.Definition
            };
            _logger.LogInformation(Resources.Feature_Workflow_Details_DataRetrievedSuccessfully);
            return await Result<WorkflowDetailsResponse>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<WorkflowDetailsResponse>.FailAsync(ex.ToString());
        }
    }
}