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

        _jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        _jsonOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    }

    public async Task<Activity> ParseActivityAsync(string json)
    {
        try
        {
            var activityJson = JsonSerializer.Deserialize<ActivityJson>(json, _jsonOptions)
                ?? throw new Exception("Deserialized ActivityJson is null");

            return new Activity
            {
                Id = Guid.NewGuid(),
                Name = activityJson.Metadata.Name,
                Namespace = activityJson.Metadata.Namespace,
                Version = activityJson.Metadata.Version,
                Description = activityJson.Spec.Description,          // short summary
                Specification = activityJson.Spec,                    // full spec
                Metadata = new Dictionary<string, object>
                {
                    ["apiVersion"] = activityJson.ApiVersion,
                    ["kind"] = activityJson.Kind,
                    ["originalJson"] = json
                },
                Labels = activityJson.Metadata.Labels ?? new(),
                Annotations = activityJson.Metadata.Annotations ?? new(),
                Owner = activityJson.Metadata.Owner,
                IsShared = activityJson.Metadata.Shared,
                Status = ActivityStatus.Active
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse Activity JSON: {ex.Message}", ex);
        }
    }

    public async Task<Workflow> ParseWorkflowAsync(string json)
    {
        try
        {
            var workflowJson = JsonSerializer.Deserialize<WorkflowJson>(json, _jsonOptions)
                ?? throw new Exception("Deserialized WorkflowJson is null");

            var workflow = new Workflow
            {
                Id = Guid.NewGuid(),
                Name = workflowJson.Metadata.Name,
                Namespace = workflowJson.Metadata.Namespace,
                Description = workflowJson.Specification.Description,
                Specification = workflowJson.Specification,   // will contain activities – we'll clear them later
                Metadata = new Dictionary<string, object>
                {
                    ["apiVersion"] = workflowJson.ApiVersion,
                    ["kind"] = workflowJson.Kind,
                    ["originalJson"] = json
                },
                Labels = workflowJson.Metadata.Labels ?? new(),
                Annotations = workflowJson.Metadata.Annotations ?? new()
            };

            // Map blueprint activities from JSON specification to Workflow.Activities
            if (workflowJson.Specification.Activities != null)
            {
                foreach (var activityJson in workflowJson.Specification.Activities)
                {
                    var activityInstance = new ActivityInstance
                    {
                        Id = activityJson.Id ?? Guid.NewGuid().ToString(),   // use provided ID or generate
                        Activity = new ActivityReference
                        {
                            Name = activityJson.Activity.Name,
                            Version = activityJson.Activity.Version ?? "latest",
                            Namespace = activityJson.Activity.Namespace ?? "default"
                        },
                        Params = activityJson.Params ?? new Dictionary<string, object>(),
                        Configuration = new ActivityConfiguration
                        {
                            Mode = activityJson.Configuration?.Mode ?? "default",
                            RunInParallel = activityJson.Configuration?.RunInParallel ?? false,
                            Priority = activityJson.Configuration?.Priority ?? 1
                        },
                        DependsOn = activityJson.DependsOn ?? new List<string>(),
                        Condition = activityJson.Condition,
                        RetryPolicy = activityJson.RetryPolicy,   // assuming JSON has a RetryPolicy object
                        TimeoutMilliseconds = activityJson.TimeoutMilliseconds
                    };

                    workflow.Activities.Add(activityInstance);
                }
            }

            // Clear activities inside specification to avoid duplication (they are now in workflow.Activities)
            workflow.Specification.Activities?.Clear();

            return workflow;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse Workflow JSON: {ex.Message}", ex);
        }
    }

    public async Task<WorkflowApplication> ParseWorkflowApplicationAsync(string json)
    {
        try
        {
            var appJson = JsonSerializer.Deserialize<WorkflowApplicationJson>(json, _jsonOptions)
                           ?? throw new Exception("Deserialized WorkflowApplicationJson is null");

            return new WorkflowApplication
            {
                Id = Guid.NewGuid(),
                Name = appJson.Metadata.Name,
                Namespace = appJson.Metadata.Namespace,
                Description = appJson.Specification.Description,
                Specification = appJson.Specification,
                Metadata = new Dictionary<string, object>
                {
                    ["apiVersion"] = appJson.ApiVersion,
                    ["kind"] = appJson.Kind,
                    ["originalJson"] = json
                },
                Labels = appJson.Metadata.Labels ?? new(),
                Annotations = appJson.Metadata.Annotations ?? new(),
                SharedContext = appJson.Specification.Environment?.Variables ?? new(),
                Owner = appJson.Metadata.Owner,
                IsShared = appJson.Metadata.Shared
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
            return JsonSerializer.Deserialize<ExecutionRequest>(json, _jsonOptions)
                ?? throw new Exception("Deserialized ExecutionRequest is null");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse ExecutionRequest JSON: {ex.Message}", ex);
        }
    }

    public string SerializeToJson<T>(T obj) => JsonSerializer.Serialize(obj, _jsonOptions);

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
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(json, _jsonOptions);

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