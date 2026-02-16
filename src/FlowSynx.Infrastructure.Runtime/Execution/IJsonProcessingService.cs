using FlowSynx.Application.Models;
using FlowSynx.Domain.Activities;
using FlowSynx.Domain.WorkflowApplications;
using FlowSynx.Domain.Workflows;

namespace FlowSynx.Infrastructure.Runtime.Execution;

public interface IJsonProcessingService
{
    Task<Activity> ParseActivityAsync(string json);
    Task<Workflow> ParseWorkflowAsync(string json);
    Task<WorkflowApplication> ParseWorkflowApplicationAsync(string json);
    Task<ExecutionRequest> ParseExecutionRequestAsync(string json);

    string SerializeToJson<T>(T obj);
    Task<ValidationResponse> ValidateJsonAsync(string json, string expectedKind);
}
