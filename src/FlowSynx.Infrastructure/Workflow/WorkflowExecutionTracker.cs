using FlowSynx.Application.Features.Workflows.Command.ExecuteWorkflow;
using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Workflow;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow;

public class WorkflowExecutionTracker : IWorkflowExecutionTracker
{
    private readonly ILogger<WorkflowExecutionTracker> _logger;
    private readonly ISystemClock _systemClock;
    private readonly IWorkflowTaskExecutionService _taskExecutionService;
    private readonly IWorkflowExecutionService _workflowExecutionService;

    public WorkflowExecutionTracker(
        ILogger<WorkflowExecutionTracker> logger, 
        ISystemClock systemClock, 
        IWorkflowTaskExecutionService taskService, 
        IWorkflowExecutionService workflowService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(systemClock);
        ArgumentNullException.ThrowIfNull(taskService);
        ArgumentNullException.ThrowIfNull(workflowService);
        _logger = logger;
        _systemClock = systemClock;
        _taskExecutionService = taskService;
        _workflowExecutionService = workflowService;
    }

    public async Task<Guid> TrackWorkflowAsync(string userId, Guid workflowId, 
        CancellationToken cancellationToken)
    {
        try
        {
            var executionEntity = new WorkflowExecutionEntity
            {
                Id = Guid.NewGuid(),
                WorkflowId = workflowId,
                UserId = userId,
                ExecutionStart = _systemClock.UtcNow,
                Status = WorkflowExecutionStatus.Running,
                TaskExecutions = new List<WorkflowTaskExecutionEntity>()
            };
            await _workflowExecutionService.Add(executionEntity, cancellationToken);
            return executionEntity.Id;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.WorkflowExecutionInitilizeFailed,
                string.Format(Resources.Workflow_Executor_WorkflowInitilizeFailed, ex.Message));
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task TrackTasksAsync(Guid workflowExecutionId, IEnumerable<WorkflowTask> tasks, 
        CancellationToken cancellationToken)
    {
        foreach (var taskName in tasks)
        {
            var taskExecutionEntity = new WorkflowTaskExecutionEntity
            {
                Id = Guid.NewGuid(),
                Name = taskName.Name,
                WorkflowExecutionId = workflowExecutionId,
                Status = WorkflowTaskExecutionStatus.Pending
            };
            await _taskExecutionService.Add(taskExecutionEntity, cancellationToken);
        }
    }

    public async Task UpdateTaskStatusAsync(Guid workflowExecutionId, string taskName, 
        WorkflowTaskExecutionStatus status, CancellationToken cancellationToken)
    {
        var entity = await _taskExecutionService.Get(workflowExecutionId, taskName, cancellationToken);
        if (entity == null)
            throw new Exception(string.Format(Resources.Workflow_ExecutionTracker_NoWorkflowTaskExecutionFound, taskName));

        entity.Status = status;
        entity.EndTime = _systemClock.UtcNow;
        await _taskExecutionService.Update(entity, cancellationToken);
    }

    public async Task UpdateWorkflowStatusAsync(string userId, Guid executionId, WorkflowExecutionStatus status, CancellationToken cancellationToken)
    {
        var entity = await _workflowExecutionService.Get(userId, executionId, cancellationToken);
        if (entity == null)
            throw new FlowSynxException((int)ErrorCode.WorkflowExecutionUpdate, 
                string.Format(Resources.Workflow_ExecutionTracker_NoWorkflowExecutionFound, executionId));

        entity.Status = status;
        entity.ExecutionEnd = _systemClock.UtcNow;
        await _workflowExecutionService.Update(entity, cancellationToken);
    }
}