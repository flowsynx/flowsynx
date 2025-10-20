using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.WorkflowTriggers.Query.WorkflowTriggerDetails;

public class WorkflowTriggerDetailsRequest : IRequest<Result<WorkflowTriggerDetailsResponse>>
{
    public required string WorkflowId { get; set; }
    public required string TriggerId { get; set; }
}