using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Application.Serialization;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Workflow;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Workflows.Command.UpdateWorkflow;

internal class UpdateWorkflowHandler : IRequestHandler<UpdateWorkflowRequest, Result<Unit>>
{
    private readonly ILogger<UpdateWorkflowHandler> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly ILocalization _localization;
    private readonly ICurrentUserService _currentUserService;

    public UpdateWorkflowHandler(
        ILogger<UpdateWorkflowHandler> logger, 
        ICurrentUserService currentUserService,
        IWorkflowService workflowService, 
        IJsonDeserializer jsonDeserializer,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _currentUserService = currentUserService;
        _workflowService = workflowService;
        _jsonDeserializer = jsonDeserializer;
        _localization = localization;
    }

    public async Task<Result<Unit>> Handle(UpdateWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflow = await _workflowService.Get(_currentUserService.UserId, workflowId, cancellationToken);
            if (workflow == null)
            {
                var message = _localization.Get("Feature_Workflow_Update_WorkflowNotFound", request.WorkflowId);
                throw new FlowSynxException((int)ErrorCode.WorkflowNotFound, message);
            }

            var workflowDefinition = _jsonDeserializer.Deserialize<WorkflowDefinition>(request.Definition);

            if (workflowDefinition == null)
                throw new FlowSynxException((int)ErrorCode.WorkflowMustBeNotEmpty,
                    _localization.Get("Features_Workflow_Update_WorkflowDefinitionMustHaveValue"));

            if (string.IsNullOrEmpty(workflowDefinition.Name))
                throw new FlowSynxException((int)ErrorCode.WorkflowNameMustHaveValue,
                    _localization.Get("Features_Workflow_Update_WorkflowNameMustHaveValue"));

            if (!string.Equals(workflow.Name, workflowDefinition.Name, StringComparison.OrdinalIgnoreCase))
            {
                var isWorkflowExist = await _workflowService.IsExist(_currentUserService.UserId, workflowDefinition.Name, cancellationToken);
                if (isWorkflowExist)
                {
                    var workflowExistMessage = _localization.Get("Features_Workflow_Update_WorkflowAlreadyExists", workflowDefinition.Name);
                    _logger.LogWarning(workflowExistMessage);
                    return await Result<Unit>.FailAsync(workflowExistMessage);
                }
            }

            workflow.Name = workflowDefinition.Name;
            workflow.Definition = request.Definition;

            await _workflowService.Update(workflow, cancellationToken);
            return await Result<Unit>.SuccessAsync(_localization.Get("Feature_Workflow_Update_AddedSuccessfully"));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}