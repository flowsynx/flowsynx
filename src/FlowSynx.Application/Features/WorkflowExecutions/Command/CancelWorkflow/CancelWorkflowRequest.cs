using MediatR;
using FlowSynx.Application.Wrapper;

namespace FlowSynx.Application.Features.WorkflowExecutions.Command.CancelWorkflow;

public class CancelWorkflowRequest : IRequest<Result<Unit>>
{
    public required string WorkflowId { get; set; }
    public required string WorkflowExecutionId { get; set; }
}