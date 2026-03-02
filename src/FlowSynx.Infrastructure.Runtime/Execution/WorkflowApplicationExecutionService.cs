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
using System.Text.Json;

namespace FlowSynx.Infrastructure.Runtime.Execution;

public class WorkflowApplicationExecutionService : IWorkflowApplicationExecutionService
{
    private readonly IWorkflowExecutionRepository _executionRepository;
    private readonly IActivityRepository _activityRepository;
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowApplicationRepository _workflowApplicationRepository;
    private readonly IJsonProcessingService _jsonService;
    private readonly IActivityExecutorFactory _executorFactory;
    private readonly IActivityCompatibilityService _activityCompatibilityService;
    private readonly IRuntimeEnvironmentProvider _runtimeEnvironmentProvider;
    private readonly ICircuitBreakerManager _circuitBreakerManager;
    private readonly ILogger<WorkflowApplicationExecutionService> _logger;

    public WorkflowApplicationExecutionService(
        IWorkflowExecutionRepository executionRepository,
        IActivityRepository activityRepository,
        IWorkflowRepository workflowRepository,
        IWorkflowApplicationRepository workflowApplicationRepository,
        IJsonProcessingService jsonService,
        IActivityExecutorFactory executorFactory,
        IActivityCompatibilityService activityCompatibilityService,
        IRuntimeEnvironmentProvider runtimeEnvironmentProvider,
        ICircuitBreakerManager circuitBreakerManager,
        ILogger<WorkflowApplicationExecutionService> logger)
    {
        _executionRepository = executionRepository;
        _activityRepository = activityRepository;
        _workflowRepository = workflowRepository;
        _workflowApplicationRepository = workflowApplicationRepository;
        _jsonService = jsonService;
        _executorFactory = executorFactory;
        _activityCompatibilityService = activityCompatibilityService;
        _runtimeEnvironmentProvider = runtimeEnvironmentProvider;
        _circuitBreakerManager = circuitBreakerManager;
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

            var env = _runtimeEnvironmentProvider.GetCurrent();
            var issues = new List<string>();
            var isCompatible = _activityCompatibilityService.IsCompatible(activity, env, out issues);
            if (isCompatible)
            {
                _logger.LogInformation("Activity is compatible with current system: {Name} v{Version}",
                    activity.Name, activity.Version);

            }
            else
            {
                _logger.LogWarning("Activity is NOT compatible with current system: {Name} v{Version}. Issues: {Issues}",
                    activity.Name, activity.Version, string.Join("; ", issues));

                throw new Exceptions.ValidationException(string.Format("Activity is NOT compatible with current system: {0} v{1}. Issues: {2}", activity.Name, activity.Version, string.Join("; ", issues)));
            }

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
                    Mode = execSettings.Mode,
                    Priority = execSettings.Priority
                },
                TimeoutMilliseconds = execSettings.TimeoutMilliseconds,
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

            var result = await ExecuteWithFaultHandlingAsync(
                executor,
                activityJson,
                activityInstance,
                safeParameters,
                safeContext,
                cancellationToken);

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
                Logs = workflowExecution.Logs.Select(log => new ExecutionLog
                {
                    Level = log.Level,
                    Message = log.Message,
                    Source = log.Source,
                    Timestamp = log.Timestamp,
                    Data = log.Data
                }).ToList(),
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

            var response = ErrorResponse(executionId, startedAt, workflowExecution, ex);
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
            var workflow = await _workflowRepository.GetByIdAsync(tenantId, userId, workflowId, cancellationToken)
                ?? throw new Exception($"Workflow not found: {workflowId}");

            // Merge workflow-level variables into context
            var mergedContext = MergeWorkflowContext(workflow.Specification.Context, context);

            var activityResults = new Dictionary<string, object>();
            var sortedActivities = TopologicalSort(workflow.Activities.ToDictionary(a => a.Id, a => a.DependsOn ?? new()));

            int index = 0;
            foreach (var actId in sortedActivities)
            {
                var act = workflow.Activities.First(a => a.Id == actId);
                executionRecord.Progress = (int)((index++ * 100) / sortedActivities.Count);
                await _executionRepository.UpdateAsync(executionRecord, cancellationToken);

                // Condition check
                if (!string.IsNullOrEmpty(act.Condition))
                {
                    var condContext = new { Variables = mergedContext, Results = activityResults };
                    if (!ConditionEvaluator.Evaluate(act.Condition, condContext))
                    {
                        _logger.LogInformation("Skipping activity {ActivityId} due to condition", act.Id);
                        continue;
                    }
                }

                try
                {
                    var activityEntity = await _activityRepository.GetByNameAndVersionAsync(
                        act.Activity.Name, act.Activity.Version ?? "latest", cancellationToken)
                        ?? throw new Exception($"Activity not found: {act.Activity.Name} (v{act.Activity.Version})");

                    var activityParams = MergeParameters(act.Params, activityResults, mergedContext);
                    var activityResponse = await ExecuteActivityAsync(
                        tenantId, userId, activityEntity.Id, activityParams, mergedContext, cancellationToken);

                    object? result = null;
                    if (activityResponse.Data.Results?.TryGetValue("result", out var tempResult) == true)
                    {
                        result = tempResult;
                        activityResults[act.Id] = result;
                    }
                    else
                    {
                        activityResults[act.Id] = null;
                    }

                    executionRecord.Logs.Add(new WorkflowExecutionLog
                    {
                        Level = "info",
                        Message = $"Activity '{act.Activity.Name}' executed",
                        Source = act.Activity.Name,
                        Timestamp = DateTime.UtcNow,
                        Data = new Dictionary<string, object> { 
                            ["result"] = result ?? (object)"null"
                        }
                    });
                }
                catch (Exception ex) when (workflow.Specification.Context?.FaultHandling?.ErrorHandling == ErrorHandlingStrategy.Ignore)
                {
                    _logger.LogWarning(ex, "Activity {ActivityId} failed but workflow continues", act.Id);
                    activityResults[act.Id] = new { error = ex.Message };
                    continue;
                }
            }

            var finalOutput = ProcessWorkflowOutput(workflow.Specification.Output, activityResults, mergedContext);

            executionRecord.Progress = 100;
            executionRecord.Status = "completed";
            executionRecord.CompletedAt = DateTime.UtcNow;
            executionRecord.DurationMilliseconds = (long)((executionRecord.CompletedAt - startedAt)?.TotalMilliseconds ?? 0);
            executionRecord.Response = finalOutput;

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
                    Phase = "succeeded",
                    Message = "Workflow execution completed",
                    Progress = 100,
                    Health = "healthy"
                },
                Results = finalOutput
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

            var response = ErrorResponse(executionId, startedAt, executionRecord, ex);
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
            var app = await _workflowApplicationRepository.GetByIdAsync(tenantId, userId, workflowApplicationId, cancellationToken) 
                ?? throw new Exception($"Workflow application not found: {workflowApplicationId}");

            var workflowResults = new List<object>();

            // Get workflow references from the specification (not from a direct collection)
            var workflowRefs = app.Specification.Workflows ?? new List<WorkflowReference>();
            for (int i = 0; i < workflowRefs.Count; i++)
            {
                var wfRef = workflowRefs[i];

                // Update progress
                executionRecord.Progress = (int)((i * 100) / workflowRefs.Count);
                await _executionRepository.UpdateAsync(executionRecord, cancellationToken);

                try
                {
                    // Load the actual workflow entity using the reference
                    var workflow = await _workflowRepository.GetByNameAsync(wfRef.Name, wfRef.Namespace ?? "default", cancellationToken) 
                        ?? throw new Exception($"Workflow not found: {wfRef.Name} (namespace: {wfRef.Namespace})");

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
                catch (Exception ex) when (app.Specification.Execution?.Mode == "continue-on-error")
                {
                    _logger.LogWarning(ex, "Workflow '{WorkflowName}' failed but application continues", wfRef.Name);
                    continue;
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

            var response = ErrorResponse(executionId, startedAt, executionRecord, ex);
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

            switch (target.Type.ToLowerInvariant())
            {
                case "activity":
                    var activity = await _activityRepository.GetByNameAndVersionAsync(
                        target.Name, target.Version ?? "latest", cancellationToken) 
                        ?? throw new Exception($"Activity not found: {target.Name}");

                    return await ExecuteActivityAsync(tenantId, userId, activity.Id, parameters, context, cancellationToken);

                case "workflow":
                    var workflow = await _workflowRepository.GetByNameAsync(
                        target.Name, target.Namespace ?? "default", cancellationToken) 
                        ?? throw new Exception($"Workflow not found: {target.Name}");

                    return await ExecuteWorkflowAsync(tenantId, userId, workflow.Id, context, cancellationToken);

                case "workflowapplication":
                    var workflowApplication = await _workflowApplicationRepository.GetByNameAsync(
                        target.Name, target.Namespace ?? "default", cancellationToken) 
                        ?? throw new Exception($"WorkflowApplication not found: {target.Name}");

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
        Guid executionId, CancellationToken cancellationToken = default)
        => await _executionRepository.GetByIdAsync(executionId, cancellationToken);

    public async Task<IEnumerable<WorkflowExecution>> GetExecutionHistoryAsync(
        string targetType, Guid targetId, CancellationToken cancellationToken = default)
        => (await _executionRepository.GetByTargetAsync(targetType, targetId)).ToList();

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

    private async Task<object> ExecuteWithFaultHandlingAsync(
        IActivityExecutor executor,
        ActivityJson activityJson,
        ActivityInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context,
        CancellationToken cancellationToken)
    {
        var faultHandling = activityJson.Spec.FaultHandling;
        if (faultHandling == null)
        {
            // No fault handling configured – simple execution with timeout
            return await ExecuteWithTimeoutAsync(
                () => executor.ExecuteAsync(activityJson, instance, parameters, context),
                instance.TimeoutMilliseconds,
                cancellationToken);
        }

        if (faultHandling.HealthCheck != null)
        {
            var healthy = await PerformHealthCheckAsync(faultHandling.HealthCheck, cancellationToken);
            if (!healthy)
            {
                _logger.LogWarning("Health check failed for activity {ActivityName}", activityJson.Metadata.Name);
                if (faultHandling.ErrorHandling == ErrorHandlingStrategy.Fallback && faultHandling.Fallback != null)
                {
                    return await ExecuteFallbackAsync(faultHandling.Fallback, cancellationToken);
                }
                throw new Exception("Health check failed");
            }
        }

        CircuitBreakerState? circuitBreakerState = null;
        if (faultHandling.CircuitBreaker != null)
        {
            var circuitBreakerId = $"{activityJson.Metadata.Namespace}:{activityJson.Metadata.Name}"; // or use activity ID
            circuitBreakerState = _circuitBreakerManager.GetOrCreate(circuitBreakerId, faultHandling.CircuitBreaker);
            if (circuitBreakerState.IsOpen)
            {
                _logger.LogWarning("Circuit breaker is OPEN for {ActivityName}, failing fast", activityJson.Metadata.Name);
                if (faultHandling.ErrorHandling == ErrorHandlingStrategy.Fallback && faultHandling.Fallback != null)
                {
                    return await ExecuteFallbackAsync(faultHandling.Fallback, cancellationToken);
                }
                throw new Exception("Circuit breaker is open");
            }
        }

        var retryPolicy = faultHandling.RetryPolicy;
        int maxAttempts = retryPolicy?.MaxAttempts ?? 1;
        int attempt = 0;
        Exception? lastException = null;

        while (attempt < maxAttempts)
        {
            attempt++;
            try
            {
                var result = await ExecuteWithTimeoutAsync(
                    () => executor.ExecuteAsync(activityJson, instance, parameters, context),
                    instance.TimeoutMilliseconds,
                    cancellationToken);

                circuitBreakerState?.RecordSuccess();
                return result;
            }
            catch (Exception ex) when (attempt < maxAttempts && retryPolicy != null)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Execution attempt {Attempt} failed for {ActivityName}", attempt, activityJson.Metadata.Name);

                circuitBreakerState?.RecordFailure();

                if (circuitBreakerState?.IsOpen == true)
                {
                    _logger.LogWarning("Circuit breaker opened after attempt {Attempt}", attempt);
                    break;
                }

                var delay = CalculateBackoff(
                    attempt,
                    TimeSpan.FromMilliseconds(retryPolicy.DelayMilliseconds),
                    retryPolicy.BackoffMultiplier,
                    retryPolicy.MaxDelayMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
        }

        if (faultHandling.ErrorHandling == ErrorHandlingStrategy.Fallback && faultHandling.Fallback != null)
        {
            _logger.LogInformation("Executing fallback for {ActivityName}", activityJson.Metadata.Name);
            return await ExecuteFallbackAsync(faultHandling.Fallback, cancellationToken);
        }

        throw lastException ?? new Exception("Execution failed after all attempts");
    }

    private async Task<bool> PerformHealthCheckAsync(HealthCheck healthCheck, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMilliseconds(healthCheck.TimeoutMilliseconds);
            var response = await httpClient.GetAsync(healthCheck.Endpoint, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<object> ExecuteFallbackAsync(Fallback fallback, CancellationToken cancellationToken)
    {
        switch (fallback.Operation?.ToLowerInvariant())
        {
            case "return_default":
                return fallback.DefaultValue;

            case "call_activity":
                var activityName = fallback.Params?["activityName"]?.ToString();
                throw new NotImplementedException("Fallback activity call not implemented");

            default:
                throw new NotSupportedException($"Fallback operation '{fallback.Operation}' not supported");
        }
    }

    private TimeSpan CalculateBackoff(int attempt, TimeSpan initialDelay, float multiplier, int maxDelayMs)
    {
        double delayMs = initialDelay.TotalMilliseconds * Math.Pow(multiplier, attempt - 1);
        delayMs = Math.Min(delayMs, maxDelayMs);
        return TimeSpan.FromMilliseconds(delayMs);
    }

    private Dictionary<string, object> MergeWorkflowContext(
        Domain.Workflows.ExecutionContext wfContext, 
        Dictionary<string, object> incoming)
    {
        var merged = new Dictionary<string, object>(incoming ?? new());
        if (wfContext?.Variables != null)
            foreach (var kvp in wfContext.Variables)
                merged.TryAdd(kvp.Key, kvp.Value);
        return merged;
    }

    private Dictionary<string, object> MergeParameters(
        Dictionary<string, object> activityParams,
        Dictionary<string, object> activityResults,
        Dictionary<string, object> workflowVariables)
    {
        var merged = new Dictionary<string, object>(workflowVariables);
        if (activityParams != null)
            foreach (var kvp in activityParams)
                merged[kvp.Key] = kvp.Value;
        merged["__results"] = activityResults;
        return merged;
    }

    private List<string> TopologicalSort(Dictionary<string, List<string>> graph)
    {
        var inDegree = graph.ToDictionary(kv => kv.Key, kv => 0);
        foreach (var deps in graph.Values)
            foreach (var dep in deps)
                if (inDegree.ContainsKey(dep))
                    inDegree[dep]++;

        var queue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var sorted = new List<string>();


        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            sorted.Add(node);
            foreach (var dep in graph[node])
            {
                inDegree[dep]--;
                if (inDegree[dep] == 0)
                    queue.Enqueue(dep);
            }
        }

        if (sorted.Count != graph.Count)
            throw new InvalidOperationException("Cycle detected in workflow dependencies");
        return sorted;
    }

    private Dictionary<string, object> ProcessWorkflowOutput(
    WorkflowOutput output,
    Dictionary<string, object> activityResults,
    Dictionary<string, object> context)
    {
        var result = new Dictionary<string, object>();
        if (output.Variables != null)
        {
            foreach (var mapping in output.Variables)
            {
                var value = ResolveMapping(mapping.Source, activityResults, context);
                if (!string.IsNullOrEmpty(mapping.Transform))
                    value = ApplyTransform(value, mapping.Transform);
                result[mapping.Name] = value;
            }
        }

        if (!string.IsNullOrEmpty(output.Path))
        {
            var json = JsonSerializer.Serialize(result);
            File.WriteAllText(output.Path, json);
        }

        return result;
    }

    private object? ResolveMapping(string source, Dictionary<string, object> activityResults, Dictionary<string, object> context)
    {
        var parts = source.Split('.');
        if (parts.Length == 0) return null;

        if (parts[0] == "context")
        {
            object current = context;
            for (int i = 1; i < parts.Length; i++)
            {
                if (current is Dictionary<string, object> dict && dict.TryGetValue(parts[i], out var val))
                    current = val;
                else
                    return null;
            }
            return current;
        }
        else
        {
            if (!activityResults.TryGetValue(parts[0], out var current))
                return null;
            for (int i = 1; i < parts.Length; i++)
            {
                if (current is Dictionary<string, object> dict && dict.TryGetValue(parts[i], out var val))
                    current = val;
                else if (current is JsonElement elem && elem.TryGetProperty(parts[i], out var prop))
                    current = prop;
                else
                    return null;
            }
            return current;
        }
    }

    private object? ApplyTransform(object? value, string transform)
    {
        // Placeholder: real implementation would use JSONPath, jq, etc.
        return value;
    }

    private ExecutionResponse ErrorResponse(string executionId, DateTime startedAt, WorkflowExecution exec, Exception ex)
    {
        return new ExecutionResponse
        {
            Metadata = new ExecutionResponseMetadata
            {
                Id = exec.Id.ToString(),
                ExecutionId = executionId,
                StartedAt = startedAt,
                CompletedAt = exec.CompletedAt,
                DurationMilliseconds = exec.DurationMilliseconds
            },
            Status = new ExecutionStatus
            {
                Phase = "failed",
                Message = ex.Message,
                Progress = exec.Progress,
                Health = "unhealthy",
                Reason = exec.ErrorCode ?? "ExecutionError"
            },
            Errors = new List<ExecutionError>
            {
                new ExecutionError
                {
                    Code = exec.ErrorCode ?? "EXECUTION_FAILED",
                    Message = ex.Message,
                    Source = exec.TargetType,
                    Timestamp = exec.CompletedAt ?? DateTime.UtcNow
                }
            }
        };
    }
}