using MediatR;
using FlowSynx.Application.Wrapper;

namespace FlowSynx.Application.Features.Workflows.Command.UpdateWorkflow;

public class UpdateWorkflowRequest : IRequest<Result<Unit>>
{
    public required string Id { get; set; }
    public required string Definition { get; set; }
}