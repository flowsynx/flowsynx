using MediatR;
using FlowSynx.Domain.Wrapper;

namespace FlowSynx.Application.Features.WorkflowExecutions.Command.RejectWorkflow;

public class RejectWorkflowRequest : IRequest<Result<Unit>>
{
    public required string WorkflowId { get; set; }
    public required string WorkflowExecutionId { get; set; }
    public required string WorkflowExecutionApprovalId { get; set; }
}