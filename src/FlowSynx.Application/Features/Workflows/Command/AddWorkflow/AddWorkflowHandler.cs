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
using Newtonsoft.Json;

namespace FlowSynx.Application.Features.Workflows.Command.AddWorkflow;

internal class AddWorkflowHandler : IRequestHandler<AddWorkflowRequest, Result<AddWorkflowResponse>>
{
    private readonly ILogger<AddWorkflowHandler> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly IWorkflowValidator _workflowValidator;
    private readonly IWorkflowSchemaValidator _workflowSchemaValidator;
    private readonly ILocalization _localization;

    public AddWorkflowHandler(
        ILogger<AddWorkflowHandler> logger,         
        IWorkflowService workflowService, 
        ICurrentUserService currentUserService, 
        IJsonDeserializer jsonDeserializer, 
        IWorkflowValidator workflowValidator,
        IWorkflowSchemaValidator workflowSchemaValidator,
        ILocalization localization)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _jsonDeserializer = jsonDeserializer ?? throw new ArgumentNullException(nameof(jsonDeserializer));
        _workflowValidator = workflowValidator ?? throw new ArgumentNullException(nameof(workflowValidator));
        _workflowSchemaValidator = workflowSchemaValidator ?? throw new ArgumentNullException(nameof(workflowSchemaValidator));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
    }

    public async Task<Result<AddWorkflowResponse>> Handle(AddWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            await _workflowSchemaValidator.ValidateAsync(request.SchemaUrl, request.Definition, cancellationToken);

            var workflowDefinition = _jsonDeserializer.Deserialize<WorkflowDefinition>(request.Definition);

            if (workflowDefinition == null)
                throw new FlowSynxException((int)ErrorCode.WorkflowMustBeNotEmpty,
                    _localization.Get("Features_Workflow_Add_WorkflowDefinitionMustHaveValue"));

            if (string.IsNullOrEmpty(workflowDefinition.Name))
                throw new FlowSynxException((int)ErrorCode.WorkflowNameMustHaveValue,
                    _localization.Get("Features_Workflow_Add_WorkflowNameMustHaveValue"));

            await _workflowValidator.ValidateAsync(workflowDefinition, cancellationToken);

            var workflowEntity = new WorkflowEntity
            {
                Id = Guid.NewGuid(),
                UserId = _currentUserService.UserId(),
                Name = workflowDefinition.Name,
                Definition = request.Definition,
                SchemaUrl = request.SchemaUrl
            };
            await _workflowService.Add(workflowEntity, cancellationToken);

            var response = new AddWorkflowResponse
            {
                Id = workflowEntity.Id,
                Name = workflowDefinition.Name,
                SchemaUrl = workflowEntity.SchemaUrl
            };
            return await Result<AddWorkflowResponse>.SuccessAsync(response,
                _localization.Get("Feature_Workflow_Add_AddedSuccessfully"));
        }
        catch (FlowSynxException ex) when (ex.ErrorCode == (int)ErrorCode.Serialization)
        {
            throw new FlowSynxException((int)ErrorCode.Serialization, 
                $"Json deserialization error: {ex.Message}");
        }
        catch (JsonReaderException ex)
        {
            throw new FlowSynxException((int)ErrorCode.Serialization, 
                $"Reader Error at Line {ex.LineNumber}, Position {ex.LinePosition}: {ex.Message}");
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<AddWorkflowResponse>.FailAsync(ex.ToString());
        }
    }
}

