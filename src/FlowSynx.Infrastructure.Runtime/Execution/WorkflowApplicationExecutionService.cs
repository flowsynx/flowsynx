using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Activities;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.WorkflowApplications;
using FlowSynx.Domain.WorkflowExecutions;
using FlowSynx.Domain.Workflows;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Runtime.Execution;

public class WorkflowApplicationExecutionService : IWorkflowApplicationExecutionService
{
    private readonly IWorkflowExecutionRepository _executionRepository;
    private readonly IActivityRepository _activityRepository;
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowApplicationRepository _workflowApplicationRepository;
    private readonly IJsonProcessingService _jsonService;
    private readonly IActivityExecutorFactory _executorFactory;
    private readonly ILogger<WorkflowApplicationExecutionService> _logger;

    public WorkflowApplicationExecutionService(
        IWorkflowExecutionRepository executionRepository,
        IActivityRepository activityRepository,
        IWorkflowRepository workflowRepository,
        IWorkflowApplicationRepository workflowApplicationRepository,
        IJsonProcessingService jsonService,
        IActivityExecutorFactory executorFactory,
        ILogger<WorkflowApplicationExecutionService> logger)
    {
        _executionRepository = executionRepository;
        _activityRepository = activityRepository;
        _workflowRepository = workflowRepository;
        _workflowApplicationRepository = workflowApplicationRepository;
        _jsonService = jsonService;
        _executorFactory = executorFactory;
        _logger = logger;
    }

    public async Task<Result<ExecutionResponse>> ExecuteActivityAsync(
        TenantId tenantId,
        string userId,
        Guid activityId,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var startedAt = DateTime.UtcNow;

        // Create execution record
        var workflowExecution = new WorkflowExecution
        {
            ExecutionId = executionId,
            TargetType = "activity",
            TargetId = activityId,
            TargetName = $"activity-{activityId}",
            Namespace = "default",
            Request = new Dictionary<string, object>
            {
                ["params"] = parameters,
                ["context"] = context
            },
            Status = "running",
            Progress = 0,
            StartedAt = startedAt,
            TriggeredBy = "system"
        };

        await _executionRepository.AddAsync(workflowExecution, cancellationToken);

        try
        {
            // Load activity
            var activity = await _activityRepository.GetByIdAsync(tenantId, userId, activityId, cancellationToken)
                ?? throw new Exception($"Activity not found: {activityId}");

            // Get execution settings from profile (retry policy now in FaultHandling)
            var execSettings = activity.Specification.ExecutionProfile.ToSettings();

            // Create a blueprint activity instance for this execution
            var activityInstance = new ActivityInstance
            {
                Id = "execution-instance", // local ID
                Activity = new ActivityReference
                {
                    Name = activity.Name,
                    Version = activity.Version,
                    Namespace = activity.Namespace
                },
                Params = parameters ?? new Dictionary<string, object>(),
                Configuration = new ActivityConfiguration
                {
                    Operation = execSettings.Operation,
                    Mode = execSettings.Mode,
                    Priority = execSettings.Priority
                },
                TimeoutMilliseconds = execSettings.TimeoutMilliseconds,
                // Retry policy from fault handling
                RetryPolicy = activity.Specification.FaultHandling?.RetryPolicy
            };

            // Get executor
            var executor = _executorFactory.CreateExecutor(activity.Specification.Executable);

            // Update progress
            workflowExecution.Progress = 50;
            await _executionRepository.UpdateAsync(workflowExecution, cancellationToken);

            var activityJson = new ActivityJson
            {
                Metadata = new ActivityMetadata
                {
                    Name = activity.Name,
                    Namespace = activity.Namespace,
                    Id = activity.Id.ToString(),
                    Version = activity.Version
                },
                Spec = activity.Specification
            };

            // Execute (with retry policy + timeout)
            var safeParameters = parameters ?? new Dictionary<string, object>();
            var safeContext = context ?? new Dictionary<string, object>();

            var result = await ExecuteWithRetryAsync(
                operation: () => ExecuteWithTimeoutAsync(
                    operation: () => executor.ExecuteAsync(activityJson, activityInstance, safeParameters, safeContext),
                    timeoutMilliseconds: activityInstance.TimeoutMilliseconds,
                    cancellationToken: cancellationToken),
                instance: activityInstance,
                cancellationToken: cancellationToken);

            // Update execution record
            workflowExecution.Progress = 100;
            workflowExecution.Status = "completed";
            workflowExecution.CompletedAt = DateTime.UtcNow;
            workflowExecution.DurationMilliseconds = (long)((workflowExecution.CompletedAt - startedAt)?.TotalMilliseconds ?? 0);
            workflowExecution.Response = new Dictionary<string, object>
            {
                ["result"] = result,
                ["success"] = true
            };

            workflowExecution.Logs.Add(new WorkflowExecutionLog
            {
                Level = "info",
                Message = $"Activity '{activity.Name}' executed successfully",
                Source = activity.Name,
                Timestamp = DateTime.UtcNow
            });

            await _executionRepository.UpdateAsync(workflowExecution, cancellationToken);

            // Return response
            var response = new ExecutionResponse
            {
                Metadata = new ExecutionResponseMetadata
                {
                    Id = workflowExecution.Id.ToString(),
                    ExecutionId = executionId,
                    StartedAt = startedAt,
                    CompletedAt = workflowExecution.CompletedAt,
                    DurationMilliseconds = workflowExecution.DurationMilliseconds
                },
                Status = new ExecutionStatus
                {
                    Phase = "succeeded",
                    Message = "Activity execution completed",
                    Progress = 100,
                    Health = "healthy"
                },
                Results = new Dictionary<string, object>
                {
                    ["result"] = result
                }
            };

            return await Result<ExecutionResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Activity execution failed for {ActivityId}", activityId);

            // Update execution record with error
            workflowExecution.Status = "failed";
            workflowExecution.CompletedAt = DateTime.UtcNow;
            workflowExecution.DurationMilliseconds = (long)((workflowExecution.CompletedAt - startedAt)?.TotalMilliseconds ?? 0);
            workflowExecution.ErrorMessage = ex.Message;
            workflowExecution.ErrorCode = "EXECUTION_FAILED";
            workflowExecution.Response = new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["success"] = false
            };

            workflowExecution.Logs.Add(new WorkflowExecutionLog
            {
                Level = "error",
                Message = ex.Message,
                Source = "execution",
                Timestamp = DateTime.UtcNow,
                Data = new Dictionary<string, object>
                {
                    ["exception"] = ex.GetType().Name,
                    ["stackTrace"] = ex.StackTrace
                }
            });

            await _executionRepository.UpdateAsync(workflowExecution, cancellationToken);

            var response = new ExecutionResponse
            {
                Metadata = new ExecutionResponseMetadata
                {
                    Id = workflowExecution.Id.ToString(),
                    ExecutionId = executionId,
                    StartedAt = startedAt,
                    CompletedAt = workflowExecution.CompletedAt,
                    DurationMilliseconds = workflowExecution.DurationMilliseconds
                },
                Status = new ExecutionStatus
                {
                    Phase = "failed",
                    Message = ex.Message,
                    Progress = 100,
                    Health = "unhealthy",
                    Reason = "ExecutionError"
                },
                Errors = new List<ExecutionError>
                {
                    new ExecutionError
                    {
                        Code = "EXECUTION_FAILED",
                        Message = ex.Message,
                        Source = "activity",
                        Timestamp = DateTime.UtcNow
                    }
                }
            };

            return await Result<ExecutionResponse>.FailAsync(response);
        }
    }

    public async Task<Result<ExecutionResponse>> ExecuteWorkflowAsync(
        TenantId tenantId,
        string userId,
        Guid workflowId,
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var startedAt = DateTime.UtcNow;

        // Create execution record
        var executionRecord = new WorkflowExecution
        {
            ExecutionId = executionId,
            TargetType = "workflow",
            TargetId = workflowId,
            TargetName = $"workflow-{workflowId}",
            Namespace = "default",
            Request = new Dictionary<string, object>
            {
                ["context"] = context
            },
            Status = "running",
            Progress = 0,
            StartedAt = startedAt,
            TriggeredBy = "system"
        };

        await _executionRepository.AddAsync(executionRecord, cancellationToken);

        try
        {
            // Load workflow with activities
            var workflow = await _workflowRepository.GetByIdAsync(tenantId, userId, workflowId, cancellationToken);
            if (workflow == null)
            {
                throw new Exception($"Workflow not found: {workflowId}");
            }

            var results = new Dictionary<string, object>();
            var activityResults = new List<object>();

            // Execute activities in the order they appear in the list (ignoring DependsOn for simplicity)
            var activities = workflow.Activities.ToList();
            int totalActivities = activities.Count;
            for (int i = 0; i < totalActivities; i++)
            {
                var activity = activities[i];

                // Update progress
                executionRecord.Progress = (int)((i * 100) / totalActivities);
                await _executionRepository.UpdateAsync(executionRecord, cancellationToken);

                try
                {
                    // Load the actual activity entity using the reference in the blueprint instance
                    var activityEntity = await _activityRepository.GetByNameAndVersionAsync(
                        activity.Activity.Name,
                        activity.Activity.Version ?? "latest",
                        cancellationToken);

                    if (activityEntity == null)
                    {
                        throw new Exception($"Activity not found: {activity.Activity.Name} (v{activity.Activity.Version})");
                    }

                    // Merge workflow context with any activity-specific parameters
                    var activityParams = activity.Params ?? new Dictionary<string, object>();

                    // Execute activity
                    var activityResult = await ExecuteActivityAsync(
                        tenantId,
                        userId,
                        activityEntity.Id,
                        activityParams,
                        context,
                        cancellationToken);

                    activityResults.Add(new
                    {
                        activityId = activity.Id, // local ID within workflow
                        activityName = activity.Activity.Name,
                        result = activityResult.Data.Results?.GetValueOrDefault("result"),
                        status = activityResult.Data.Status.Phase
                    });

                    executionRecord.Logs.Add(new WorkflowExecutionLog
                    {
                        Level = "info",
                        Message = $"Activity '{activity.Activity.Name}' executed successfully",
                        Source = activity.Activity.Name,
                        Timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    executionRecord.Logs.Add(new WorkflowExecutionLog
                    {
                        Level = "error",
                        Message = $"Activity '{activity.Activity.Name}' failed: {ex.Message}",
                        Source = activity.Activity.Name,
                        Timestamp = DateTime.UtcNow
                    });

                    // Check if we should continue based on workflow error handling
                    var errorHandling = workflow.Specification.Context?.FaultHandling;
                    if (errorHandling?.ErrorHandling == ErrorHandlingStrategy.Propagate)
                    {
                        throw;
                    }
                    // else continue with other activities
                }
            }

            // Update execution record
            executionRecord.Progress = 100;
            executionRecord.Status = "completed";
            executionRecord.CompletedAt = DateTime.UtcNow;
            executionRecord.DurationMilliseconds = (long)((executionRecord.CompletedAt - startedAt)?.TotalMilliseconds ?? 0);
            executionRecord.Response = new Dictionary<string, object>
            {
                ["activityResults"] = activityResults,
                ["success"] = true
            };

            await _executionRepository.UpdateAsync(executionRecord, cancellationToken);

            // Return response
            var response = new ExecutionResponse
            {
                Metadata = new ExecutionResponseMetadata
                {
                    Id = executionRecord.Id.ToString(),
                    ExecutionId = executionId,
                    StartedAt = startedAt,
                    CompletedAt = executionRecord.CompletedAt,
                    DurationMilliseconds = executionRecord.DurationMilliseconds
                },
                Status = new ExecutionStatus
                {
                    Phase = "succeeded",
                    Message = "Workflow execution completed",
                    Progress = 100,
                    Health = "healthy"
                },
                Results = new Dictionary<string, object>
                {
                    ["activityResults"] = activityResults
                }
            };

            return await Result<ExecutionResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow execution failed for {WorkflowId}", workflowId);

            executionRecord.Status = "failed";
            executionRecord.CompletedAt = DateTime.UtcNow;
            executionRecord.DurationMilliseconds = (long)((executionRecord.CompletedAt - startedAt)?.TotalMilliseconds ?? 0);
            executionRecord.ErrorMessage = ex.Message;
            executionRecord.ErrorCode = "EXECUTION_FAILED";

            executionRecord.Logs.Add(new WorkflowExecutionLog
            {
                Level = "error",
                Message = ex.Message,
                Source = "workflow",
                Timestamp = DateTime.UtcNow
            });

            await _executionRepository.UpdateAsync(executionRecord, cancellationToken);

            var response = new ExecutionResponse
            {
                Metadata = new ExecutionResponseMetadata
                {
                    Id = executionRecord.Id.ToString(),
                    ExecutionId = executionId,
                    StartedAt = startedAt,
                    CompletedAt = executionRecord.CompletedAt,
                    DurationMilliseconds = executionRecord.DurationMilliseconds
                },
                Status = new ExecutionStatus
                {
                    Phase = "failed",
                    Message = ex.Message,
                    Progress = executionRecord.Progress,
                    Health = "unhealthy",
                    Reason = "ExecutionError"
                },
                Errors = new List<ExecutionError>
                {
                    new ExecutionError
                    {
                        Code = "EXECUTION_FAILED",
                        Message = ex.Message,
                        Source = "workflow",
                        Timestamp = DateTime.UtcNow
                    }
                }
            };

            return await Result<ExecutionResponse>.FailAsync(response);
        }
    }

    public async Task<Result<ExecutionResponse>> ExecuteWorkflowApplicationAsync(
        TenantId tenantId,
        string userId,
        Guid workflowApplicationId,
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var startedAt = DateTime.UtcNow;

        // Create execution record
        var executionRecord = new WorkflowExecution
        {
            ExecutionId = executionId,
            TargetType = "workflowApplication",
            TargetId = workflowApplicationId,
            TargetName = $"workflowApplication-{workflowApplicationId}",
            Namespace = "default",
            Request = new Dictionary<string, object>
            {
                ["context"] = context
            },
            Status = "running",
            Progress = 0,
            StartedAt = startedAt,
            TriggeredBy = "system"
        };

        await _executionRepository.AddAsync(executionRecord, cancellationToken);

        try
        {
            // Load workflow application
            var workflowApplication = await _workflowApplicationRepository.GetByIdAsync(tenantId, userId, workflowApplicationId, cancellationToken);
            if (workflowApplication == null)
            {
                throw new Exception($"Workflow application not found: {workflowApplicationId}");
            }

            var workflowResults = new List<object>();

            // Get workflow references from the specification (not from a direct collection)
            var workflowRefs = workflowApplication.Specification.Workflows ?? new List<WorkflowReference>();
            int totalWorkflows = workflowRefs.Count;
            for (int i = 0; i < totalWorkflows; i++)
            {
                var wfRef = workflowRefs[i];

                // Update progress
                executionRecord.Progress = (int)((i * 100) / totalWorkflows);
                await _executionRepository.UpdateAsync(executionRecord, cancellationToken);

                try
                {
                    // Load the actual workflow entity using the reference
                    var workflow = await _workflowRepository.GetByNameAsync(
                        wfRef.Name,
                        wfRef.Namespace ?? "default",
                        cancellationToken);

                    if (workflow == null)
                    {
                        throw new Exception($"Workflow not found: {wfRef.Name} (namespace: {wfRef.Namespace})");
                    }

                    // Merge application context with any workflow-specific parameters from the reference
                    var mergedContext = new Dictionary<string, object>(context);
                    if (wfRef.Params != null)
                    {
                        foreach (var kvp in wfRef.Params)
                        {
                            mergedContext[kvp.Key] = kvp.Value;
                        }
                    }

                    var workflowResult = await ExecuteWorkflowAsync(
                        tenantId,
                        userId,
                        workflow.Id,
                        mergedContext,
                        cancellationToken);

                    workflowResults.Add(new
                    {
                        workflowId = workflow.Id,
                        workflowName = workflow.Name,
                        result = workflowResult,
                        status = workflowResult.Data.Status.Phase
                    });

                    executionRecord.Logs.Add(new WorkflowExecutionLog
                    {
                        Level = "info",
                        Message = $"Workflow '{workflow.Name}' executed successfully",
                        Source = workflow.Name,
                        Timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    executionRecord.Logs.Add(new WorkflowExecutionLog
                    {
                        Level = "error",
                        Message = $"Workflow '{wfRef.Name}' failed: {ex.Message}",
                        Source = wfRef.Name,
                        Timestamp = DateTime.UtcNow
                    });

                    // Check workflow application execution strategy
                    var executionStrategy = workflowApplication.Specification.Execution?.Mode;
                    if (executionStrategy == "stop-on-error")
                    {
                        throw;
                    }
                    // else continue with other workflows
                }
            }

            // Update execution record
            executionRecord.Progress = 100;
            executionRecord.Status = "completed";
            executionRecord.CompletedAt = DateTime.UtcNow;
            executionRecord.DurationMilliseconds = (long)((executionRecord.CompletedAt - startedAt)?.TotalMilliseconds ?? 0);
            executionRecord.Response = new Dictionary<string, object>
            {
                ["workflowResults"] = workflowResults,
                ["success"] = true
            };

            await _executionRepository.UpdateAsync(executionRecord, cancellationToken);

            // Return response
            var response = new ExecutionResponse
            {
                Metadata = new ExecutionResponseMetadata
                {
                    Id = executionRecord.Id.ToString(),
                    ExecutionId = executionId,
                    StartedAt = startedAt,
                    CompletedAt = executionRecord.CompletedAt,
                    DurationMilliseconds = executionRecord.DurationMilliseconds
                },
                Status = new ExecutionStatus
                {
                    Phase = "succeeded",
                    Message = "Workflow application execution completed",
                    Progress = 100,
                    Health = "healthy"
                },
                Results = new Dictionary<string, object>
                {
                    ["workflowResults"] = workflowResults
                }
            };

            return await Result<ExecutionResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow application execution failed for {WorkflowApplicationId}", workflowApplicationId);

            executionRecord.Status = "failed";
            executionRecord.CompletedAt = DateTime.UtcNow;
            executionRecord.DurationMilliseconds = (long)((executionRecord.CompletedAt - startedAt)?.TotalMilliseconds ?? 0);
            executionRecord.ErrorMessage = ex.Message;
            executionRecord.ErrorCode = "EXECUTION_FAILED";

            executionRecord.Logs.Add(new WorkflowExecutionLog
            {
                Level = "error",
                Message = ex.Message,
                Source = "workflowApplication",
                Timestamp = DateTime.UtcNow
            });

            await _executionRepository.UpdateAsync(executionRecord, cancellationToken);

            var response = new ExecutionResponse
            {
                Metadata = new ExecutionResponseMetadata
                {
                    Id = executionRecord.Id.ToString(),
                    ExecutionId = executionId,
                    StartedAt = startedAt,
                    CompletedAt = executionRecord.CompletedAt,
                    DurationMilliseconds = executionRecord.DurationMilliseconds
                },
                Status = new ExecutionStatus
                {
                    Phase = "failed",
                    Message = ex.Message,
                    Progress = executionRecord.Progress,
                    Health = "unhealthy",
                    Reason = "ExecutionError"
                },
                Errors = new List<ExecutionError>
                {
                    new ExecutionError
                    {
                        Code = "EXECUTION_FAILED",
                        Message = ex.Message,
                        Source = "workflowApplication",
                        Timestamp = DateTime.UtcNow
                    }
                }
            };

            return await Result<ExecutionResponse>.FailAsync(response);
        }
    }

    public async Task<Result<ExecutionResponse>> ExecuteRequestAsync(
        TenantId tenantId,
        string userId,
        ExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var target = request.Spec.Target;
            var parameters = request.Spec.Params ?? new Dictionary<string, object>();
            var context = new Dictionary<string, object>();

            // Merge environment and context
            if (request.Spec.Environment != null)
            {
                foreach (var kvp in request.Spec.Environment)
                {
                    context[kvp.Key] = kvp.Value;
                }
            }
            if (request.Spec.Context != null)
            {
                foreach (var kvp in request.Spec.Context)
                {
                    context[kvp.Key] = kvp.Value;
                }
            }

            switch (target.Type.ToLower())
            {
                case "activity":
                    var activity = await _activityRepository.GetByNameAndVersionAsync(
                        target.Name, target.Version ?? "latest", cancellationToken);
                    if (activity == null)
                        throw new Exception($"Activity not found: {target.Name}");

                    return await ExecuteActivityAsync(tenantId, userId, activity.Id, parameters, context, cancellationToken);

                case "workflow":
                    var workflow = await _workflowRepository.GetByNameAsync(
                        target.Name, target.Namespace ?? "default", cancellationToken);
                    if (workflow == null)
                        throw new Exception($"Workflow not found: {target.Name}");

                    return await ExecuteWorkflowAsync(tenantId, userId, workflow.Id, context, cancellationToken);

                case "workflowApplication":
                    var workflowApplication = await _workflowApplicationRepository.GetByNameAsync(
                        target.Name, target.Namespace ?? "default", cancellationToken);
                    if (workflowApplication == null)
                        throw new Exception($"WorkflowApplication not found: {target.Name}");

                    return await ExecuteWorkflowApplicationAsync(tenantId, userId, workflowApplication.Id, context, cancellationToken);

                default:
                    throw new Exception($"Unknown target type: {target.Type}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Execution request failed");
            throw;
        }
    }

    public async Task<WorkflowExecution?> GetWorkflowExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        return await _executionRepository.GetByIdAsync(executionId, cancellationToken);
    }

    public async Task<IEnumerable<WorkflowExecution>> GetExecutionHistoryAsync(
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        return (await _executionRepository.GetByTargetAsync(targetType, targetId))
            .ToList();
    }

    private async Task<object?> ExecuteWithTimeoutAsync(
        Func<Task<object?>> operation,
        int? timeoutMilliseconds,
        CancellationToken cancellationToken)
    {
        if (!timeoutMilliseconds.HasValue || timeoutMilliseconds < 1)
            return await operation();

        var executionTask = operation();
        var timeoutTask = Task.Delay(timeoutMilliseconds.Value, cancellationToken);

        var completed = await Task.WhenAny(executionTask, timeoutTask);
        if (completed == executionTask)
            return await executionTask;

        // If the delay completed due to cancellation, propagate cancellation (not a timeout).
        cancellationToken.ThrowIfCancellationRequested();

        throw new TimeoutException($"Activity execution exceeded timeout of {timeoutMilliseconds}ms.");
    }

    private async Task<object> ExecuteWithRetryAsync(
        Func<Task<object?>> operation,
        ActivityInstance instance,
        CancellationToken cancellationToken)
    {
        var policy = instance.RetryPolicy ?? new RetryPolicy(); // fallback if null

        var maxAttempts = Math.Max(1, policy.MaxAttempts);
        var baseDelayMs = Math.Max(0, policy.DelayMilliseconds);
        var backoffMultiplier = policy.BackoffMultiplier < 1.0f ? 1.0f : policy.BackoffMultiplier;
        var maxDelayMs = policy.MaxDelayMilliseconds < 0 ? 0 : policy.MaxDelayMilliseconds;

        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                lastException = ex;

                var delayMs = CalculateDelayMilliseconds(
                    retryIndex: attempt - 1,
                    baseDelayMs: baseDelayMs,
                    multiplier: backoffMultiplier,
                    maxDelayMs: maxDelayMs);

                _logger.LogWarning(
                    ex,
                    "Activity {ActivityInstanceId} failed on attempt {Attempt}/{MaxAttempts}. Retrying in {DelayMilliseconds}ms.",
                    instance.Id,
                    attempt,
                    maxAttempts,
                    delayMs);

                if (delayMs > 0)
                    await Task.Delay(delayMs, cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;
                throw;
            }
        }

        throw lastException ?? new Exception("Activity execution failed after retries.");
    }

    private static int CalculateDelayMilliseconds(int retryIndex, int baseDelayMs, float multiplier, int maxDelayMs)
    {
        if (baseDelayMs <= 0)
            return 0;

        var factor = Math.Pow(multiplier, retryIndex);
        var rawDelay = baseDelayMs * factor;

        var delay = rawDelay >= int.MaxValue ? int.MaxValue : (int)Math.Round(rawDelay);
        delay = Math.Max(0, delay);

        if (maxDelayMs > 0)
            delay = Math.Min(delay, maxDelayMs);

        return delay;
    }
}