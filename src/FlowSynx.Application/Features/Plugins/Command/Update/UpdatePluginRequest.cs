using MediatR;
using FlowSynx.Application.Wrapper;

namespace FlowSynx.Application.Features.Plugins.Command.Update;

public class UpdatePluginRequest : IRequest<Result<Unit>>
{
    public required string Type { get; set; }
    public required string OldVersion { get; set; }
    public required string NewVersion { get; set; }
}