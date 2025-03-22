using Cronos;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Entities.Trigger;
using FlowSynx.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow;

public class WorkflowTimeBasedTriggerProcessor : IWorkflowTriggerProcessor
{
    private readonly ILogger<WorkflowTimeBasedTriggerProcessor> _logger;
    private readonly IWorkflowTriggerService _workflowTriggerService;
    private readonly IWorkflowExecutor _workflowExecutor;
    private readonly ISystemClock _systemClock;

    public WorkflowTimeBasedTriggerProcessor(ILogger<WorkflowTimeBasedTriggerProcessor> logger, 
        IWorkflowTriggerService workflowTriggerService, IWorkflowExecutor workflowExecutor,
        ISystemClock systemClock)
    {
        _logger = logger;
        _workflowTriggerService = workflowTriggerService;
        _workflowExecutor = workflowExecutor;
        _systemClock = systemClock;
    }

    public async Task ProcessTriggersAsync(CancellationToken cancellationToken)
    {
        var triggers = await _workflowTriggerService.All(WorkflowTriggerType.TimeBased, cancellationToken);

        DateTime now = _systemClock.UtcNow;
        foreach (var trigger in triggers)
        {
            var cron = trigger.Properties.TryGetValue("cron", out var cronValue);
            if (cronValue is not string cornExpr)
            {
                _logger.LogError($"The entered time-based trigger is not valid for workflow with Id '{trigger.WorkflowId}'");
                return;
            }

            if (ShouldRunTrigger(cornExpr))
            {
                await _workflowExecutor.ExecuteAsync(trigger.UserId, trigger.WorkflowId, cancellationToken);
            }
        }
    }

    private bool ShouldRunTrigger(string cronExpression)
    {
        var schedule = CronExpression.Parse(cronExpression);
        var nextOccurrence = schedule.GetNextOccurrence(DateTime.UtcNow.AddMinutes(-1));
        return nextOccurrence <= _systemClock.UtcNow;
    }
}