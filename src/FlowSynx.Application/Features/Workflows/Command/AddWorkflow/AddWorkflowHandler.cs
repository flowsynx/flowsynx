using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Application.Serialization;
using FlowSynx.Application.Services;
using FlowSynx.Application.Workflow;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain;
using FlowSynx.Domain.Workflow;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlowSynx.Application.Features.Workflows.Command.AddWorkflow;

internal class AddWorkflowHandler : IRequestHandler<AddWorkflowRequest, Result<AddWorkflowResponse>>
{
    private readonly ILogger<AddWorkflowHandler> _logger;
    private readonly ITransactionService _transactionService;
    private readonly IWorkflowService _workflowService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly IWorkflowValidator _workflowValidator;
    private readonly IWorkflowSchemaValidator _workflowSchemaValidator;
    private readonly ILocalization _localization;

    public AddWorkflowHandler(
        ILogger<AddWorkflowHandler> logger, 
        ITransactionService transactionService,
        IWorkflowService workflowService, 
        ICurrentUserService currentUserService, 
        IJsonDeserializer jsonDeserializer, 
        IWorkflowValidator workflowValidator,
        IWorkflowSchemaValidator workflowSchemaValidator,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(transactionService);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(jsonDeserializer);
        ArgumentNullException.ThrowIfNull(workflowValidator);
        ArgumentNullException.ThrowIfNull(workflowSchemaValidator);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _transactionService = transactionService;
        _workflowService = workflowService;
        _currentUserService = currentUserService;
        _jsonDeserializer = jsonDeserializer;
        _workflowValidator = workflowValidator;
        _workflowSchemaValidator = workflowSchemaValidator ?? throw new ArgumentNullException(nameof(workflowSchemaValidator));
        _localization = localization;
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

            _workflowValidator.Validate(workflowDefinition);

            var isWorkflowExist = await _workflowService.IsExist(_currentUserService.UserId, workflowDefinition.Name, cancellationToken);
            if (isWorkflowExist)
            {
                var workflowExistMessage = _localization.Get("Features_Workflow_Add_WorkflowAlreadyExists", workflowDefinition.Name);
                var errorMessage = new ErrorMessage((int)ErrorCode.WorkflowCheckExistence, workflowExistMessage);
                _logger.LogWarning(errorMessage.ToString());
                return await Result<AddWorkflowResponse>.FailAsync(errorMessage.ToString());
            }

            var workflowEntity = new WorkflowEntity
            {
                Id = Guid.NewGuid(),
                UserId = _currentUserService.UserId,
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
