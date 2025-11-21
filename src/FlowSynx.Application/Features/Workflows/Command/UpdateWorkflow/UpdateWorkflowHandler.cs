using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Localizations;
using FlowSynx.Domain;
using FlowSynx.Application.Serialization;
using FlowSynx.Application.Services;
using FlowSynx.Application.Workflow;
using FlowSynx.Domain.Wrapper;
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
    private readonly IWorkflowSchemaValidator _workflowSchemaValidator;

    public UpdateWorkflowHandler(
        ILogger<UpdateWorkflowHandler> logger, 
        ICurrentUserService currentUserService,
        IWorkflowService workflowService, 
        IJsonDeserializer jsonDeserializer,
        IWorkflowSchemaValidator workflowSchemaValidator,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(jsonDeserializer);
        ArgumentNullException.ThrowIfNull(workflowSchemaValidator);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _currentUserService = currentUserService;
        _workflowService = workflowService;
        _jsonDeserializer = jsonDeserializer;
        _workflowSchemaValidator = workflowSchemaValidator;
        _localization = localization;
    }

    public async Task<Result<Unit>> Handle(UpdateWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflow = await _workflowService.Get(_currentUserService.UserId(), workflowId, cancellationToken);
            if (workflow == null)
            {
                var message = _localization.Get("Feature_Workflow_Update_WorkflowNotFound", request.WorkflowId);
                throw new FlowSynxException((int)ErrorCode.WorkflowNotFound, message);
            }

            string? normalizedSchemaUrl;

            if (request.SchemaUrl is null)
            {
                normalizedSchemaUrl = workflow.SchemaUrl;
            }
            else if (string.IsNullOrWhiteSpace(request.SchemaUrl))
            {
                normalizedSchemaUrl = null;
            }
            else
            {
                normalizedSchemaUrl = request.SchemaUrl;
            }

            await _workflowSchemaValidator.ValidateAsync(normalizedSchemaUrl, request.Definition, cancellationToken);

            var workflowDefinition = _jsonDeserializer.Deserialize<WorkflowDefinition>(request.Definition);

            if (workflowDefinition == null)
                throw new FlowSynxException((int)ErrorCode.WorkflowMustBeNotEmpty,
                    _localization.Get("Features_Workflow_Update_WorkflowDefinitionMustHaveValue"));

            if (string.IsNullOrEmpty(workflowDefinition.Name))
                throw new FlowSynxException((int)ErrorCode.WorkflowNameMustHaveValue,
                    _localization.Get("Features_Workflow_Update_WorkflowNameMustHaveValue"));

            workflow.Name = workflowDefinition.Name;
            workflow.Definition = request.Definition;
            workflow.SchemaUrl = normalizedSchemaUrl;

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

