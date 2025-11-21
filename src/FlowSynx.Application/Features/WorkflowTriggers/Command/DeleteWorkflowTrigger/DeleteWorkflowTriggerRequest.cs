using MediatR;
using FlowSynx.Domain.Wrapper;

namespace FlowSynx.Application.Features.WorkflowTriggers.Command.DeleteWorkflowTrigger;

public class DeleteWorkflowTriggerRequest : IRequest<Result<Unit>>
{
    public required string WorkflowId { get; set; }
    public required string TriggerId { get; set; }
}