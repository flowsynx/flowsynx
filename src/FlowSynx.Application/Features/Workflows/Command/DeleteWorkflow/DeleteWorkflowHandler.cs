using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Workflow;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Workflows.Command.DeleteWorkflow;

internal class DeleteWorkflowHandler : IRequestHandler<DeleteWorkflowRequest, Result<Unit>>
{
    private readonly ILogger<DeleteWorkflowHandler> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly ILocalization _localization;
    private readonly ICurrentUserService _currentUserService;

    public DeleteWorkflowHandler(
        ILogger<DeleteWorkflowHandler> logger, 
        ICurrentUserService currentUserService,
        IWorkflowService workflowService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _currentUserService = currentUserService;
        _workflowService = workflowService;
        _localization = localization;
    }

    public async Task<Result<Unit>> Handle(DeleteWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflow = await _workflowService.Get(_currentUserService.UserId, workflowId, cancellationToken);
            if (workflow == null)
            {
                var message = _localization.Get("Features_Workflow_Delete_WorkflowCouldNotBeFound", request.WorkflowId);
                throw new FlowSynxException((int)ErrorCode.WorkflowNotFound, message);
            }

            await _workflowService.Delete(workflow, cancellationToken);
            return await Result<Unit>.SuccessAsync(_localization.Get("Feature_Workflow_Delete_DeletedSuccessfully"));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}