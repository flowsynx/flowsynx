using Cronos;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Services;
using FlowSynx.Application.Workflow;
using FlowSynx.Domain.Trigger;

namespace FlowSynx.Infrastructure.Workflow;

public class WorkflowTimeBasedTriggerProcessor : IWorkflowTriggerProcessor
{
    private readonly ILogger<WorkflowTimeBasedTriggerProcessor> _logger;
    private readonly IWorkflowTriggerService _workflowTriggerService;
    private readonly IWorkflowExecutor _workflowExecutor;
    private readonly ISystemClock _systemClock;

    public WorkflowTimeBasedTriggerProcessor(
        ILogger<WorkflowTimeBasedTriggerProcessor> logger,
        IWorkflowTriggerService workflowTriggerService,
        IWorkflowExecutor workflowExecutor,
        ISystemClock systemClock)
    {
        _logger = logger;
        _workflowTriggerService = workflowTriggerService;
        _workflowExecutor = workflowExecutor;
        _systemClock = systemClock;
    }

    public async Task ProcessTriggersAsync(CancellationToken cancellationToken)
    {
        var triggers = await _workflowTriggerService.ActiveTriggers(
            WorkflowTriggerType.TimeBased, cancellationToken);

        foreach (var trigger in triggers)
        {
            if (!TryParseCronExpression(trigger, out var cronExpression))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(cronExpression) && IsTriggerDue(cronExpression))
            {
                await ExecuteWorkflowAsync(trigger, cancellationToken);
            }
        }
    }

    private bool TryParseCronExpression(WorkflowTriggerEntity triggerEntity, out string? cronExpression)
    {
        cronExpression = null;

        if (!triggerEntity.Properties.TryGetValue("cron", out var cronValue) || cronValue is not string expr)
        {
            _logger.LogError($"Missing or invalid 'cron' expression for workflow with ID '{triggerEntity.WorkflowId}'.");
            return false;
        }

        cronExpression = expr;
        return true;
    }

    private bool IsTriggerDue(string cronExpression)
    {
        var schedule = CronExpression.Parse(cronExpression);
        var lastCheckedTime = DateTime.UtcNow.AddMinutes(-1);
        var nextOccurrence = schedule.GetNextOccurrence(lastCheckedTime);

        return nextOccurrence.HasValue && nextOccurrence <= _systemClock.UtcNow;
    }

    private async Task ExecuteWorkflowAsync(WorkflowTriggerEntity trigger, CancellationToken cancellationToken)
    {
        try
        {
            await _workflowExecutor.ExecuteAsync(trigger.UserId, trigger.WorkflowId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to execute workflow with ID '{trigger.WorkflowId}' for user '{trigger.UserId}'.");
        }
    }
}
