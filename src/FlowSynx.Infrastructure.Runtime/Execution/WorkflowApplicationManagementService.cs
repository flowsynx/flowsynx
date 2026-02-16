using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Activities;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.WorkflowApplications;
using FlowSynx.Domain.Workflows;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Runtime.Execution;

public class WorkflowApplicationManagementService : IWorkflowApplicationManagementService
{
    private readonly IActivityRepository _activityRepository;
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowApplicationRepository _workflowApplicationRepository;
    private readonly IJsonProcessingService _jsonService;
    private readonly IWorkflowApplicationExecutionService _executionService;
    private readonly ILogger<WorkflowApplicationManagementService> _logger;

    public WorkflowApplicationManagementService(
        IActivityRepository activityRepository,
        IWorkflowRepository workflowRepository,
        IWorkflowApplicationRepository workflowApplicationRepository,
        IJsonProcessingService jsonService,
        IWorkflowApplicationExecutionService executionService,
        ILogger<WorkflowApplicationManagementService> logger)
    {
        _activityRepository = activityRepository;
        _workflowRepository = workflowRepository;
        _workflowApplicationRepository = workflowApplicationRepository;
        _jsonService = jsonService;
        _executionService = executionService;
        _logger = logger;
    }

    public async Task<Activity> RegisterActivityAsync(
        string userId, 
        string json, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate JSON
            var validation = await _jsonService.ValidateJsonAsync(json, "Gene");
            if (!validation.Status.Valid)
            {
                throw new FlowSynx.Infrastructure.Runtime.Exceptions.ValidationException("Gene validation failed", validation.Errors);
            }

            // Parse JSON
            var activity = await _jsonService.ParseActivityAsync(json);

            // Check if already exists
            var existing = await _activityRepository.GetByNameAndVersionAsync(
                activity.Name, activity.Version, cancellationToken);
            if (existing != null)
            {
                // Update existing
                existing.Specification = activity.Specification;
                existing.Description = activity.Description;
                existing.Metadata = activity.Metadata;
                existing.Labels = activity.Labels;
                existing.Annotations = activity.Annotations;

                await _activityRepository.UpdateAsync(existing, cancellationToken);

                _logger.LogInformation("Updated existing activity: {Name} v{Version}",
                    activity.Name, activity.Version);

                return existing;
            }
            else
            {
                // Add new
                activity.UserId = userId;
                await _activityRepository.AddAsync(activity, cancellationToken);

                _logger.LogInformation("Registered new activity: {Name} v{Version}",
                    activity.Name, activity.Version);
                return activity;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register activity");
            throw;
        }
    }

    public async Task<Workflow> RegisterWorkflowAsync(
        string userId, 
        string json, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate JSON
            var validation = await _jsonService.ValidateJsonAsync(json, "Chromosome");
            if (!validation.Status.Valid)
            {
                throw new FlowSynx.Infrastructure.Runtime.Exceptions.ValidationException("Chromosome validation failed", validation.Errors);
            }

            // Parse JSON
            var workflow = await _jsonService.ParseWorkflowAsync(json);

            // Check if already exists
            var existing = await _workflowRepository.GetByNameAsync(
                workflow.Name, workflow.Namespace,
                cancellationToken);

            if (existing != null)
            {
                // Update existing
                existing.Specification = workflow.Specification;
                existing.Description = workflow.Description;
                existing.Metadata = workflow.Metadata;
                existing.Labels = workflow.Labels;
                existing.Annotations = workflow.Annotations;
                existing.Activities = workflow.Activities;

                await _workflowRepository.UpdateAsync(existing, cancellationToken);

                _logger.LogInformation("Updated existing workflow: {Name}", workflow.Name);
                return existing;
            }
            else
            {
                // Add new
                await _workflowRepository.AddAsync(workflow, cancellationToken);

                _logger.LogInformation("Registered new workflow: {Name}", workflow.Name);

                return workflow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register workflow");
            throw;
        }
    }

    public async Task<WorkflowApplication> RegisterWorkflowApplicationAsync(
        string userId, 
        string json, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate JSON
            var validation = await _jsonService.ValidateJsonAsync(json, "Genome");
            if (!validation.Status.Valid)
            {
                throw new FlowSynx.Infrastructure.Runtime.Exceptions.ValidationException("Genome validation failed", validation.Errors);
            }

            // Parse JSON
            var genome = await _jsonService.ParseWorkflowApplicationAsync(json);

            // Check if already exists
            var existing = await _workflowApplicationRepository.GetByNameAsync(
                genome.Name, genome.Namespace, cancellationToken);

            if (existing != null)
            {
                // Update existing
                existing.Specification = genome.Specification;
                existing.Description = genome.Description;
                existing.Metadata = genome.Metadata;
                existing.Labels = genome.Labels;
                existing.Annotations = genome.Annotations;
                existing.SharedContext = genome.SharedContext;

                await _workflowApplicationRepository.UpdateAsync(existing, cancellationToken);

                _logger.LogInformation("Updated existing workflow application: {Name}", genome.Name);
                return existing;
            }
            else
            {
                // Add new
                await _workflowApplicationRepository.AddAsync(genome, cancellationToken);

                _logger.LogInformation("Registered new workflow application: {Name}", genome.Name);

                return genome;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register genome");
            throw;
        }
    }

    public async Task<ValidationResponse> ValidateJsonAsync(
        string userId, 
        string json, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var jsonElement = System.Text.Json.JsonDocument.Parse(json).RootElement;

            if (jsonElement.TryGetProperty("kind", out var kindElement))
            {
                var kind = kindElement.GetString();
                return await _jsonService.ValidateJsonAsync(json, kind);
            }

            return new ValidationResponse
            {
                Metadata = new ValidationMetadata
                {
                    ValidatedAt = DateTimeOffset.UtcNow,
                    Resource = "unknown"
                },
                Status = new ValidationStatus
                {
                    Valid = false,
                    Score = 0,
                    Message = "Missing 'kind' field"
                }
            };
        }
        catch (Exception ex)
        {
            return new ValidationResponse
            {
                Metadata = new ValidationMetadata
                {
                    ValidatedAt = DateTimeOffset.UtcNow,
                    Resource = "error"
                },
                Status = new ValidationStatus
                {
                    Valid = false,
                    Score = 0,
                    Message = ex.Message
                }
            };
        }
    }

    public async Task<IEnumerable<Activity>> SearchActivitiesAsync(
        TenantId tenantId,
        string userId, 
        string searchTerm, 
        CancellationToken cancellationToken = default)
    {
        return await _activityRepository.SearchAsync(tenantId, userId, searchTerm, cancellationToken);
    }

    public async Task<IEnumerable<Workflow>> GetWorkflowsByApplicationIdAsync(
        string userId, 
        Guid workflowApplicationId, 
        CancellationToken cancellationToken = default)
    {
        return await _workflowRepository.GetByWorkflowApplicationIdAsync(workflowApplicationId, cancellationToken);
    }

    public async Task<IEnumerable<WorkflowApplication>> GetWorkflowApplicationsByOwnerAsync(
        string userId,
        string owner, 
        CancellationToken cancellationToken = default)
    {
        return await _workflowApplicationRepository.GetByOwnerAsync(owner, cancellationToken);
    }

    public async Task<ExecutionResponse> ExecuteJsonAsync(
        TenantId tenantId,
        string userId, 
        string json, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var jsonElement = System.Text.Json.JsonDocument.Parse(json).RootElement;

            if (jsonElement.TryGetProperty("kind", out var kindElement))
            {
                var kind = kindElement.GetString();

                if (kind == "ExecutionRequest")
                {
                    var request = await _jsonService.ParseExecutionRequestAsync(json);
                    return await _executionService.ExecuteRequestAsync(tenantId, userId, request);
                }
                else if (kind == "Activity")
                {
                    var activity = await RegisterActivityAsync(userId, json);
                    return new ExecutionResponse
                    {
                        Metadata = new ExecutionResponseMetadata
                        {
                            Id = Guid.NewGuid().ToString(),
                            ExecutionId = $"register-{Guid.NewGuid()}",
                            StartedAt = DateTimeOffset.UtcNow,
                            CompletedAt = DateTimeOffset.UtcNow
                        },
                        Status = new ExecutionStatus
                        {
                            Phase = "succeeded",
                            Message = $"Activity '{activity.Name}' registered successfully",
                            Progress = 100,
                            Health = "healthy"
                        }
                    };
                }
                else if (kind == "Workflow")
                {
                    var workflow = await RegisterWorkflowAsync(userId, json);
                    return new ExecutionResponse
                    {
                        Metadata = new ExecutionResponseMetadata
                        {
                            Id = Guid.NewGuid().ToString(),
                            ExecutionId = $"register-{Guid.NewGuid()}",
                            StartedAt = DateTimeOffset.UtcNow,
                            CompletedAt = DateTimeOffset.UtcNow
                        },
                        Status = new ExecutionStatus
                        {
                            Phase = "succeeded",
                            Message = $"Workflow '{workflow.Name}' registered successfully",
                            Progress = 100,
                            Health = "healthy"
                        }
                    };
                }
                else if (kind == "WorkflowApplication")
                {
                    var workflowApplication = await RegisterWorkflowApplicationAsync(userId, json);
                    return new ExecutionResponse
                    {
                        Metadata = new ExecutionResponseMetadata
                        {
                            Id = Guid.NewGuid().ToString(),
                            ExecutionId = $"register-{Guid.NewGuid()}",
                            StartedAt = DateTimeOffset.UtcNow,
                            CompletedAt = DateTimeOffset.UtcNow
                        },
                        Status = new ExecutionStatus
                        {
                            Phase = "succeeded",
                            Message = $"Workflow Application '{workflowApplication.Name}' registered successfully",
                            Progress = 100,
                            Health = "healthy"
                        }
                    };
                }
            }

            throw new FlowSynx.Infrastructure.Runtime.Exceptions.ValidationException("Unknown or missing 'kind' field");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute JSON");
            throw;
        }
    }

    public async Task<ExecutionResponse> GetExecutionResultAsync(
        string userId, 
        Guid executionId, 
        CancellationToken cancellationToken = default)
    {
        var executionRecord = await _executionService.GetWorkflowExecutionAsync(executionId, cancellationToken);

        if (executionRecord == null)
        {
            throw new NotFoundException($"Execution record not found: {executionId}");
        }

        return new ExecutionResponse
        {
            Metadata = new ExecutionResponseMetadata
            {
                Id = executionRecord.Id.ToString(),
                ExecutionId = executionRecord.ExecutionId,
                StartedAt = executionRecord.StartedAt,
                CompletedAt = executionRecord.CompletedAt,
                DurationMilliseconds = executionRecord.DurationMilliseconds
            },
            Status = new ExecutionStatus
            {
                Phase = executionRecord.Status,
                Message = executionRecord.ErrorMessage ?? "Execution completed",
                Progress = executionRecord.Progress,
                Health = executionRecord.Status == "completed" ? "healthy" : "unhealthy",
                Reason = executionRecord.ErrorCode
            },
            Results = executionRecord.Response,
            Errors = executionRecord.Status == "failed" ? new List<ExecutionError>
                {
                    new ExecutionError
                    {
                        Code = executionRecord.ErrorCode ?? "UNKNOWN_ERROR",
                        Message = executionRecord.ErrorMessage ?? "Execution failed",
                        Source = executionRecord.TargetType,
                        Timestamp = executionRecord.CompletedAt ?? DateTime.UtcNow
                    }
                } : new List<ExecutionError>(),
            Logs = executionRecord.Logs?.Select(log => new Application.Models.ExecutionLog
            {
                Level = log.Level,
                Message = log.Message,
                Source = log.Source,
                Timestamp = log.Timestamp,
                Data = log.Data
            }).ToList() ?? new List<Application.Models.ExecutionLog>(),
            Artifacts = executionRecord.Artifacts?.Select(artifact => new Application.Models.ExecutionArtifact
            {
                Name = artifact.Name,
                Type = artifact.Type,
                Content = artifact.Content,
                Size = artifact.Size,
                CreatedAt = artifact.CreatedAt
            }).ToList() ?? new List<Application.Models.ExecutionArtifact>()
        };
    }
}