using MediatR;
using FlowSynx.Core.Wrapper;

namespace FlowSynx.Core.Features.Workflows.Command.Delete;

public class DeleteWorkflowRequest : IRequest<Result<Unit>>
{
    public required string Id { get; set; }
}