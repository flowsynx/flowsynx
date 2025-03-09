using FlowSynx.Core.Wrapper;
using MediatR;

namespace FlowSynx.Core.Features.Workflows.Query.Details;

public class WorkflowDetailsRequest : IRequest<Result<WorkflowDetailsResponse>>
{
    public required string Name { get; set; }
}