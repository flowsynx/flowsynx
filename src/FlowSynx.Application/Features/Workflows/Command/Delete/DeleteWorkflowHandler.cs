using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Workflow;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Workflows.Command.Delete;

internal class DeleteWorkflowHandler : IRequestHandler<DeleteWorkflowRequest, Result<Unit>>
{
    private readonly ILogger<DeleteWorkflowHandler> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly ICurrentUserService _currentUserService;

    public DeleteWorkflowHandler(ILogger<DeleteWorkflowHandler> logger, ICurrentUserService currentUserService,
        IWorkflowService workflowService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(workflowService);
        _logger = logger;
        _currentUserService = currentUserService;
        _workflowService = workflowService;
    }

    public async Task<Result<Unit>> Handle(DeleteWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAuthenticationIsRequired, Resources.Authentication_Access_Denied);

            var workflowId = Guid.Parse(request.Id);
            var workflow = await _workflowService.Get(_currentUserService.UserId, workflowId, cancellationToken);
            if (workflow == null)
            {
                var message = string.Format(Resources.Features_Workflow_Delete_WorkflowCouldNotBeFound, request.Id);
                throw new FlowSynxException((int)ErrorCode.WorkflowNotFound, message);
            }

            await _workflowService.Delete(workflow, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.Feature_Workflow_Delete_DeletedSuccessfully);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}