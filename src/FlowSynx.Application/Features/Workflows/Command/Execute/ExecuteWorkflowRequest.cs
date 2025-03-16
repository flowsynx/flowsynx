using MediatR;
using FlowSynx.Application.Wrapper;

namespace FlowSynx.Application.Features.Workflows.Command.Execute;

public class ExecuteWorkflowRequest : IRequest<Result<Unit>>
{
    public required Guid WorkflowId { get; set; }
}