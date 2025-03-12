using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Workflows.Query.Details;

public class WorkflowDetailsRequest : IRequest<Result<WorkflowDetailsResponse>>
{
    public required string Id { get; set; }
}