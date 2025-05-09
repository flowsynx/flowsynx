using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Workflows.Query.WorkflowTriggersList;

public class WorkflowTriggersListRequest : IRequest<Result<IEnumerable<WorkflowTriggersListResponse>>>
{
    public required string WorkflowId { get; set; }
}