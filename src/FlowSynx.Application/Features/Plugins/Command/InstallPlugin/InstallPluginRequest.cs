using MediatR;
using FlowSynx.Application.Wrapper;

namespace FlowSynx.Application.Features.Plugins.Command.InstallPlugin;

public class InstallPluginRequest : IRequest<Result<Unit>>
{
    public required string Type { get; set; }
    public string Version { get; set; } = "latest";
}