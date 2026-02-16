using FlowSynx.Application.Models;
using FlowSynx.Domain.Activities;
using FlowSynx.Domain.WorkflowApplications;
using FlowSynx.Domain.Workflows;
using System.Text.Json;

namespace FlowSynx.Infrastructure.Runtime.Execution;

public class JsonProcessingService : IJsonProcessingService
{
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonProcessingService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }

    public async Task<Activity> ParseActivityAsync(string json)
    {
        try
        {
            var activityJson = JsonSerializer.Deserialize<ActivityJson>(json, _jsonOptions);

            // Convert to domain entity
            return new Activity
            {
                Id = Guid.NewGuid(),
                Name = activityJson.Metadata.Name,
                Namespace = activityJson.Metadata.Namespace,
                Version = activityJson.Metadata.Version,
                Description = activityJson.Specification.Description,
                Specification = activityJson.Specification,
                Metadata = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["apiVersion"] = activityJson.ApiVersion,
                    ["kind"] = activityJson.Kind,
                    ["originalJson"] = json
                },
                Labels = activityJson.Metadata.Labels ?? new System.Collections.Generic.Dictionary<string, string>(),
                Annotations = activityJson.Metadata.Annotations ?? new System.Collections.Generic.Dictionary<string, string>(),
                Owner = activityJson.Metadata.Owner,
                IsShared = activityJson.Metadata.Shared,
                Status = "active"
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse GeneBlueprint JSON: {ex.Message}", ex);
        }
    }

    public async Task<Workflow> ParseWorkflowAsync(string json)
    {
        try
        {
            var workflowJson = JsonSerializer.Deserialize<WorkflowJson>(json, _jsonOptions);

            // Convert to domain entity
            var workflow = new Workflow
            {
                Id = Guid.NewGuid(),
                Name = workflowJson.Metadata.Name,
                Namespace = workflowJson.Metadata.Namespace,
                Description = workflowJson.Specification.Description,
                Specification = workflowJson.Specification,
                Metadata = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["apiVersion"] = workflowJson.ApiVersion,
                    ["kind"] = workflowJson.Kind,
                    ["originalJson"] = json
                },
                Labels = workflowJson.Metadata.Labels ?? new System.Collections.Generic.Dictionary<string, string>(),
                Annotations = workflowJson.Metadata.Annotations ?? new System.Collections.Generic.Dictionary<string, string>()
            };

            // Parse gene instances
            if (workflowJson.Specification.Activities != null)
            {
                int order = 0;
                foreach (var activityJson in workflowJson.Specification.Activities)
                {
                    var activityInstance = new Domain.ActivityInstances.ActivityInstance
                    {
                        Id = Guid.NewGuid(),
                        ActivityId = activityJson.Activity.Name,
                        Parameters = activityJson.Parameters ?? new System.Collections.Generic.Dictionary<string, object>(),
                        Configuration = new Domain.ActivityInstances.ActivityConfiguration
                        {
                            Operation = activityJson.Configuration?.Operation,
                            Mode = activityJson.Configuration?.Mode ?? "default",
                            RunInParallel = activityJson.Configuration?.RunInParallel ?? false,
                            Priority = activityJson.Configuration?.Priority ?? 1,
                            //Timeout = activityJson.Configuration?.Timeout,
                            //Retry = activityJson.Configuration?.Retry
                        },
                        Metadata = new System.Collections.Generic.Dictionary<string, object>
                        {
                            ["id"] = activityJson.Id,
                            ["dependsOn"] = activityJson.DependsOn,
                            ["condition"] = activityJson.Condition
                        },
                        Order = order++
                    };

                    workflow.Activities.Add(activityInstance);
                }
            }

            return workflow;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse Chromosome JSON: {ex.Message}", ex);
        }
    }

    public async Task<WorkflowApplication> ParseWorkflowApplicationAsync(string json)
    {
        try
        {
            var workflowApplicationJson = JsonSerializer.Deserialize<WorkflowApplicationJson>(json, _jsonOptions);

            // Convert to domain entity
            return new WorkflowApplication
            {
                Id = Guid.NewGuid(),
                Name = workflowApplicationJson.Metadata.Name,
                Namespace = workflowApplicationJson.Metadata.Namespace,
                Description = workflowApplicationJson.Specification.Description,
                Specification = workflowApplicationJson.Specification,
                Metadata = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["apiVersion"] = workflowApplicationJson.ApiVersion,
                    ["kind"] = workflowApplicationJson.Kind,
                    ["originalJson"] = json
                },
                Labels = workflowApplicationJson.Metadata.Labels ?? new System.Collections.Generic.Dictionary<string, string>(),
                Annotations = workflowApplicationJson.Metadata.Annotations ?? new System.Collections.Generic.Dictionary<string, string>(),
                SharedContext = workflowApplicationJson.Specification.Environment?.Variables ?? new System.Collections.Generic.Dictionary<string, object>(),
                Owner = workflowApplicationJson.Metadata.Owner,
                IsShared = workflowApplicationJson.Metadata.Shared
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse WorkflowApplication JSON: {ex.Message}", ex);
        }
    }

    public async Task<ExecutionRequest> ParseExecutionRequestAsync(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ExecutionRequest>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse ExecutionRequest JSON: {ex.Message}", ex);
        }
    }

    public string SerializeToJson<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, _jsonOptions);
    }

    public async Task<ValidationResponse> ValidateJsonAsync(string json, string expectedKind)
    {
        var response = new ValidationResponse
        {
            Metadata = new ValidationMetadata
            {
                ValidatedAt = DateTimeOffset.UtcNow,
                Resource = expectedKind
            },
            Status = new ValidationStatus
            {
                Valid = true,
                Score = 100,
                Message = "Validation passed"
            }
        };

        try
        {
            // Parse to check JSON structure
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(json, _jsonOptions);

            // Check if it has required fields
            if (jsonElement.TryGetProperty("kind", out var kindElement))
            {
                var kind = kindElement.GetString();
                if (kind != expectedKind)
                {
                    response.Status.Valid = false;
                    response.Status.Score = 0;
                    response.Status.Message = $"Expected kind '{expectedKind}', got '{kind}'";
                    response.Errors.Add(new ValidationError
                    {
                        Field = "kind",
                        Message = $"Expected '{expectedKind}', got '{kind}'",
                        Code = "KIND_MISMATCH",
                        Severity = "error"
                    });
                }
            }
            else
            {
                response.Status.Valid = false;
                response.Status.Score = 0;
                response.Status.Message = "Missing 'kind' field";
                response.Errors.Add(new ValidationError
                {
                    Field = "$",
                    Message = "Missing 'kind' field",
                    Code = "MISSING_KIND",
                    Severity = "error"
                });
            }
        }
        catch (JsonException ex)
        {
            response.Status.Valid = false;
            response.Status.Score = 0;
            response.Status.Message = "Invalid JSON";
            response.Errors.Add(new ValidationError
            {
                Field = "$",
                Message = ex.Message,
                Code = "INVALID_JSON",
                Severity = "fatal"
            });
        }
        catch (Exception ex)
        {
            response.Status.Valid = false;
            response.Status.Score = 0;
            response.Status.Message = "Validation failed";
            response.Errors.Add(new ValidationError
            {
                Field = "$",
                Message = ex.Message,
                Code = "VALIDATION_ERROR",
                Severity = "fatal"
            });
        }

        return response;
    }
}