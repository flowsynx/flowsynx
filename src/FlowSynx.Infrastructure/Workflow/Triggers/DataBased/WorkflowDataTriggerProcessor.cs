using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.Services;
using FlowSynx.Application.Workflow;
using FlowSynx.Domain.Trigger;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow.Triggers.DataBased;

/// <summary>
/// Periodically polls configured data sources and enqueues workflow executions for detected changes.
/// </summary>
public class WorkflowDataTriggerProcessor : IWorkflowTriggerProcessor
{
    private static readonly ConcurrentDictionary<Guid, DataTriggerState> TriggerStates = new();

    private readonly ILogger<WorkflowDataTriggerProcessor> _logger;
    private readonly IWorkflowTriggerService _workflowTriggerService;
    private readonly IWorkflowOrchestrator _workflowOrchestrator;
    private readonly IWorkflowExecutionQueue _workflowExecutionQueue;
    private readonly IDataChangeProviderFactory _dataChangeProviderFactory;
    private readonly ISystemClock _systemClock;
    private readonly ILocalization _localization;

    public WorkflowDataTriggerProcessor(
        ILogger<WorkflowDataTriggerProcessor> logger,
        IWorkflowTriggerService workflowTriggerService,
        IWorkflowOrchestrator workflowOrchestrator,
        IWorkflowExecutionQueue workflowExecutionQueue,
        IDataChangeProviderFactory dataChangeProviderFactory,
        ISystemClock systemClock,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowTriggerService);
        ArgumentNullException.ThrowIfNull(workflowOrchestrator);
        ArgumentNullException.ThrowIfNull(workflowExecutionQueue);
        ArgumentNullException.ThrowIfNull(dataChangeProviderFactory);
        ArgumentNullException.ThrowIfNull(systemClock);
        ArgumentNullException.ThrowIfNull(localization);

        _logger = logger;
        _workflowTriggerService = workflowTriggerService;
        _workflowOrchestrator = workflowOrchestrator;
        _workflowExecutionQueue = workflowExecutionQueue;
        _dataChangeProviderFactory = dataChangeProviderFactory;
        _systemClock = systemClock;
        _localization = localization;
    }

    public string Name { get; } = "Data-Based Trigger Processor";
    public TimeSpan Interval { get; } = TimeSpan.FromSeconds(5);

    public async Task ProcessTriggersAsync(CancellationToken cancellationToken)
    {
        var triggers = await _workflowTriggerService.GetActiveTriggersByTypeAsync(
            WorkflowTriggerType.DataBased,
            cancellationToken);

        foreach (var trigger in triggers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!DataTriggerConfiguration.TryCreate(trigger, out var configuration, out var error))
            {
                _logger.LogError(_localization.Get(
                    "Workflow_DataBased_TriggerProcessor_InvalidConfiguration", trigger.Id, error ?? "Unknown"));
                continue;
            }

            var config = configuration!;
            var state = TriggerStates.GetOrAdd(config.TriggerId, _ => new DataTriggerState());
            var now = _systemClock.UtcNow;

            if (!state.ShouldPoll(now, config.PollInterval))
                continue;

            try
            {
                var provider = _dataChangeProviderFactory.Resolve(config.ProviderKey);
                var changes = await provider.GetChangesAsync(config, state, cancellationToken);

                if (changes.Count == 0)
                {
                    state.MarkPolled(now);
                    continue;
                }

                foreach (var change in changes)
                {
                    await ExecuteWorkflowAsync(config, change, cancellationToken);
                }

                state.MarkPolled(now);
                state.Update(changes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _localization.Get(
                    "Workflow_DataBased_TriggerProcessor_PollFailed",
                    config.WorkflowId,
                    config.TriggerId,
                    ex.Message));
            }
        }
    }

    private async Task ExecuteWorkflowAsync(
        DataTriggerConfiguration configuration,
        DataChangeEvent change,
        CancellationToken cancellationToken)
    {
        var triggerEntity = configuration.Trigger;

        try
        {
            var triggerContext = BuildTriggerPayload(configuration, change);

            var execution = await _workflowOrchestrator.CreateWorkflowExecutionAsync(
                triggerEntity.UserId,
                triggerEntity.WorkflowId,
                cancellationToken);

            await _workflowExecutionQueue.QueueExecutionAsync(new ExecutionQueueRequest(
                triggerEntity.UserId,
                triggerEntity.WorkflowId,
                execution.Id,
                cancellationToken,
                triggerContext), cancellationToken);

            _logger.LogInformation(_localization.Get(
                "Workflow_DataBased_TriggerProcessor_QueuedExecution",
                triggerEntity.WorkflowId,
                triggerEntity.Id,
                change.Operation,
                change.Table));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _localization.Get(
                "Workflow_DataBased_TriggerProcessor_FailedDispatch",
                triggerEntity.WorkflowId,
                triggerEntity.Id,
                ex.Message));
        }
    }

    private static WorkflowTrigger BuildTriggerPayload(
        DataTriggerConfiguration configuration,
        DataChangeEvent change)
    {
        var additional = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["workflowId"] = configuration.WorkflowId,
            ["triggerId"] = configuration.TriggerId
        };

        var eventPayload = change.ToPayload(additional);

        var properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["event"] = eventPayload,
            ["provider"] = configuration.ProviderKey,
            ["tables"] = configuration.Tables.ToArray(),
            ["operations"] = configuration.Operations.Select(op => op.ToString()).ToArray()
        };

        if (configuration.Columns.Count > 0)
            properties["columns"] = configuration.Columns.ToArray();

        if (configuration.ProviderSettings.Count > 0)
            properties["settings"] = configuration.ProviderSettings.ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);

        return new WorkflowTrigger
        {
            Type = WorkflowTriggerType.DataBased,
            Properties = properties
        };
    }
}
