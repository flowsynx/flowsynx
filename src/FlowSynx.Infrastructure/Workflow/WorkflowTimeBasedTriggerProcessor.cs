using Cronos;
using FlowSynx.Application.Features.Workflows.Command.Execute;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Entities.Trigger;
using FlowSynx.Domain.Interfaces;

namespace FlowSynx.Infrastructure.Workflow;

public class WorkflowTimeBasedTriggerProcessor : IWorkflowTriggerProcessor
{
    public readonly IWorkflowTriggerService _workflowTriggerService;
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly IWorkflowExecutor _workflowExecutor;

    public WorkflowTimeBasedTriggerProcessor(IWorkflowTriggerService workflowTriggerService,
        IJsonDeserializer jsonDeserializer, IWorkflowExecutor workflowExecutor)
    {
        _workflowTriggerService = workflowTriggerService;
        _jsonDeserializer = jsonDeserializer;
        _workflowExecutor = workflowExecutor;
    }

    public async Task ProcessTriggersAsync(CancellationToken cancellationToken)
    {
        var triggers = await _workflowTriggerService.All(WorkflowTriggerType.TimeBased, cancellationToken);

        foreach (var trigger in triggers)
        {
            var triggerDetails = _jsonDeserializer.Deserialize<WorkflowTimeBasedTriggerDetails>(trigger.Details);

            var cronExpression = triggerDetails.Cron;
            var nextRun = CronExpression.Parse(cronExpression).GetNextOccurrence(DateTime.Now);

            if (nextRun.HasValue && nextRun.Value <= DateTime.Now)
            {
                // Trigger the workflow
                await _workflowExecutor.ExecuteAsync(trigger.UserId, trigger.WorkflowId, cancellationToken);
            }
        }
    }

}