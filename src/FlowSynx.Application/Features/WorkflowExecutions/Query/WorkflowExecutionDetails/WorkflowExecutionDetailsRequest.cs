using FlowSynx.Domain.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionDetails;

public class WorkflowExecutionDetailsRequest : IRequest<Result<WorkflowExecutionDetailsResponse>>
{
    public required string WorkflowId { get; set; }
    public required string WorkflowExecutionId { get; set; }
}