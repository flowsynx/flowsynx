using MediatR;
using FlowSynx.Application.Wrapper;

namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class ExecuteWorkflowRequest : IRequest<Result<ExecuteWorkflowResponse>>
{
    public required string WorkflowId { get; set; }
}