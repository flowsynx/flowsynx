using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.Workflow;
using FlowSynx.Domain.Trigger;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow.Triggers.HttpBased;

public class WorkflowHttpTriggerProcessor : IWorkflowTriggerProcessor
{
    private readonly ILogger<WorkflowHttpTriggerProcessor> _logger;
    private readonly IWorkflowTriggerService _workflowTriggerService;
    private readonly IWorkflowOrchestrator _workflowOrchestrator;
    private readonly IWorkflowExecutionQueue _workflowExecutionQueue;
    private readonly IWorkflowHttpListener _workflowHttpListener;
    private readonly ILocalization _localization;

    public WorkflowHttpTriggerProcessor(
        ILogger<WorkflowHttpTriggerProcessor> logger,
        IWorkflowTriggerService workflowTriggerService,
        IWorkflowOrchestrator workflowOrchestrator,
        IWorkflowExecutionQueue workflowExecutionQueue,
        IWorkflowHttpListener workflowHttpListener,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowTriggerService);
        ArgumentNullException.ThrowIfNull(workflowOrchestrator);
        ArgumentNullException.ThrowIfNull(workflowExecutionQueue);
        ArgumentNullException.ThrowIfNull(workflowHttpListener);
        ArgumentNullException.ThrowIfNull(localization);

        _logger = logger;
        _workflowTriggerService = workflowTriggerService;
        _workflowOrchestrator = workflowOrchestrator;
        _workflowExecutionQueue = workflowExecutionQueue;
        _workflowHttpListener = workflowHttpListener;
        _localization = localization;
    }

    public string Name { get; } = "HTTP Trigger Processor";
    public TimeSpan Interval { get; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Registers HTTP trigger routes and starts listening.
    /// </summary>
    public async Task ProcessTriggersAsync(CancellationToken cancellationToken)
    {
        var triggers = await _workflowTriggerService.GetActiveTriggersByTypeAsync(
            WorkflowTriggerType.Http, cancellationToken);

        foreach (var trigger in triggers)
        {
            if (!TryParseHttpTrigger(trigger, out var method, out var route))
                continue;

            try
            {
                _workflowHttpListener.RegisterRoute(trigger.UserId, method, route, async (request) =>
                {
                    await ExecuteWorkflowAsync(trigger, request.Body, cancellationToken);
                });

                _logger.LogInformation(_localization.Get(
                    "Workflow_Http_TriggerProcessor_RegisteredRoute",
                    route, method, trigger.WorkflowId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _localization.Get(
                    "Workflow_Http_TriggerProcessor_FailedRegistration",
                    trigger.WorkflowId, trigger.UserId));
            }
        }
    }

    private bool TryParseHttpTrigger(
        WorkflowTriggerEntity triggerEntity,
        out string method,
        out string route)
    {
        method = "POST";
        route = string.Empty;

        if (!triggerEntity.Properties.TryGetValue("route", out var routeValue) ||
            routeValue is not string r || string.IsNullOrWhiteSpace(r))
        {
            _logger.LogError(_localization.Get(
                "Workflow_Http_TriggerProcessor_InvalidRoute", triggerEntity.WorkflowId));
            return false;
        }

        if (triggerEntity.Properties.TryGetValue("method", out var methodValue) &&
            methodValue is string m && !string.IsNullOrWhiteSpace(m))
        {
            method = m.ToUpperInvariant();
        }

        route = r.StartsWith("/") ? r : "/" + r;
        return true;
    }

    private async Task ExecuteWorkflowAsync(
        WorkflowTriggerEntity trigger,
        string? requestBody,
        CancellationToken cancellationToken)
    {
        try
        {
            var executionEntity = await _workflowOrchestrator.CreateWorkflowExecutionAsync(
                trigger.UserId,
                trigger.WorkflowId,
                cancellationToken);

            await _workflowExecutionQueue.QueueExecutionAsync(new ExecutionQueueRequest(
                trigger.UserId,
                trigger.WorkflowId,
                executionEntity.Id,
                cancellationToken), cancellationToken);

            _logger.LogInformation(_localization.Get(
                "Workflow_Http_TriggerProcessor_Triggered", trigger.WorkflowId, trigger.UserId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _localization.Get(
                "Workflow_Http_TriggerProcessor_FailedExecution", trigger.WorkflowId, trigger.UserId));
        }
    }
}