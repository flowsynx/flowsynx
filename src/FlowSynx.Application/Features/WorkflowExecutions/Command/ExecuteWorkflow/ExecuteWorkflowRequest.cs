using MediatR;
using FlowSynx.Domain.Wrapper;

namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class ExecuteWorkflowRequest : IRequest<Result<ExecuteWorkflowResponse>>
{
    public required string WorkflowId { get; set; }
}