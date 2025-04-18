using MediatR;
using FlowSynx.Application.Wrapper;

namespace FlowSynx.Application.Features.Plugins.Command.Delete;

public class DeletePluginRequest : IRequest<Result<Unit>>
{
    public required string Type { get; set; }
    public required string Version { get; set; }
}