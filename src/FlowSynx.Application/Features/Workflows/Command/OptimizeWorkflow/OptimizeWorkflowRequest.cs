using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Workflows.Command.OptimizeWorkflow;

public class OptimizeWorkflowRequest : IRequest<Result<OptimizeWorkflowResponse>>
{
    public required string WorkflowId { get; init; }
    public bool ApplyChanges { get; init; } = false;
    public string? SchemaUrl { get; init; }
}