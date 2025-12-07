using MediatR;
using FlowSynx.Domain.Wrapper;

namespace FlowSynx.Application.Features.Plugins.Command.UpdatePlugin;

public class UpdatePluginRequest : IRequest<Result<Unit>>
{
    public required string Type { get; set; }
}