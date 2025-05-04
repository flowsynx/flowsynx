using FlowSynx.Application.Features.Workflows.Command.Execute;
using FlowSynx.Application.Models;
using FlowSynx.Application.Serialization;
using FlowSynx.Application.Workflow;
using FlowSynx.Domain.Workflow;
using FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow;

public class WorkflowExecutor : IWorkflowExecutor
{
    private readonly ILogger<WorkflowExecutor> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly IWorkflowExecutionTracker _executionTracker;
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly IWorkflowValidator _workflowValidator;
    private readonly IWorkflowOrchestrator _workflowOrchestrator;
    private readonly IErrorHandlingResolver _errorHandlingResolver;

    public WorkflowExecutor(
        ILogger<WorkflowExecutor> logger, 
        IWorkflowService workflowService,
        IWorkflowExecutionTracker executionTracker, 
        IJsonDeserializer jsonDeserializer, 
        IWorkflowValidator workflowValidator, 
        IWorkflowOrchestrator workflowOrchestrator,
        IErrorHandlingResolver errorHandlingResolver)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(executionTracker);
        ArgumentNullException.ThrowIfNull(jsonDeserializer);
        ArgumentNullException.ThrowIfNull(workflowValidator);
        _logger = logger;
        _workflowService = workflowService;
        _executionTracker = executionTracker;
        _jsonDeserializer = jsonDeserializer;
        _workflowValidator = workflowValidator;
        _workflowOrchestrator = workflowOrchestrator;
        _errorHandlingResolver = errorHandlingResolver;
    }

    public async Task ExecuteAsync(string userId, Guid workflowId, 
        CancellationToken cancellationToken)
    {
        var workflow = await GetWorkflow(userId, workflowId, cancellationToken);

        var executionId = await _executionTracker
            .TrackWorkflowAsync(workflow.UserId, workflow.Id, cancellationToken);

        try
        {
            var definition = DeserializeAndValidateWorkflow(workflow.Definition);

            await _executionTracker
                .TrackTasksAsync(executionId, definition.Tasks, cancellationToken).ConfigureAwait(false);

            await _workflowOrchestrator
                 .ExecuteWorkflowAsync(workflow.UserId, executionId, definition, cancellationToken)
                 .ConfigureAwait(false);

            await _executionTracker
                  .UpdateWorkflowStatusAsync(userId, executionId, WorkflowExecutionStatus.Completed, cancellationToken)
                  .ConfigureAwait(false);

            _logger.LogInformation(string.Format(Resources.Workflow_Executor_CompletedSuccessfully, workflow.Id));
        }
        catch (Exception ex)
        {
            await _executionTracker
                  .UpdateWorkflowStatusAsync(userId, executionId, WorkflowExecutionStatus.Failed, cancellationToken)
                  .ConfigureAwait(false);

            throw new FlowSynxException((int)ErrorCode.WorkflowFailedExecution, 
                string.Format(Resources.Workflow_Executor_ErrorInExecution, ex.Message));
        }
    }

    private WorkflowDefinition DeserializeAndValidateWorkflow(string definitionJson)
    {
        var definition = _jsonDeserializer.Deserialize<WorkflowDefinition>(definitionJson);
        _errorHandlingResolver.Resolve(definition);
        _workflowValidator.Validate(definition);
        return definition;
    }
      
    private async Task<WorkflowEntity> GetWorkflow(string userId, Guid workflowId, CancellationToken cancellationToken)
    {
        try
        {
            var workFlowEntity = await _workflowService.Get(userId, workflowId, cancellationToken);
            if (workFlowEntity != null) 
                return workFlowEntity;

            var message = string.Format(Resources.Workflow_Executor_WorkflowCouldNotBeFound, workflowId);
            throw new FlowSynxException((int)ErrorCode.WorkflowNotFound, message);
        }
        catch (Exception ex)
        {
            var messageMessage = new ErrorMessage((int)ErrorCode.WorkflowGetItem, 
                string.Format(Resources.Workflow_Executor_GetWorkflowFailed, ex.Message));
            _logger.LogError(messageMessage.ToString());
            throw new FlowSynxException(messageMessage);
        }
    }
}