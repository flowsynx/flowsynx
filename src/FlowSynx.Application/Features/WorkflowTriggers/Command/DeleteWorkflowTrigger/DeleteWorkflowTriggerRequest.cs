using MediatR;
using FlowSynx.Application.Wrapper;

namespace FlowSynx.Application.Features.Workflows.Command.DeleteWorkflowTrigger;

public class DeleteWorkflowTriggerRequest : IRequest<Result<Unit>>
{
    public required string WorkflowId { get; set; }
    public required string TriggerId { get; set; }
}