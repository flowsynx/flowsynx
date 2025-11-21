using FlowSynx.Domain.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowTaskExecutionDetails;

public class WorkflowTaskExecutionDetailsRequest : IRequest<Result<WorkflowTaskExecutionDetailsResponse>>
{
    public required string WorkflowId { get; set; }
    public required string WorkflowExecutionId { get; set; }
    public required string WorkflowTaskExecutionId { get; set; }
}