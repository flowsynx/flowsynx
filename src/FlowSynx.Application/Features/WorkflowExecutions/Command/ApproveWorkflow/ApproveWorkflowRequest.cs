using MediatR;
using FlowSynx.Application.Wrapper;

namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ApproveWorkflow;

public class ApproveWorkflowRequest : IRequest<Result<Unit>>
{
    public required string WorkflowId { get; set; }
    public required string WorkflowExecutionId { get; set; }
    public required string WorkflowExecutionApprovalId { get; set; }
}