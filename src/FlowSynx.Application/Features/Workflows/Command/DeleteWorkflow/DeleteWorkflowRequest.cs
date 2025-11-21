using MediatR;
using FlowSynx.Domain.Wrapper;

namespace FlowSynx.Application.Features.Workflows.Command.DeleteWorkflow;

public class DeleteWorkflowRequest : IRequest<Result<Unit>>
{
    public required string WorkflowId { get; set; }
}