using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Workflows.Query.WorkflowDetails;

public class WorkflowDetailsRequest : IRequest<Result<WorkflowDetailsResponse>>
{
    public required string WorkflowId { get; set; }
}