using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Config.Command.Delete;

public class DeleteConfigRequest : IRequest<Result<DeleteConfigResponse>>
{
    public required string Name { get; set; }
}