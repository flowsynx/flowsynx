using MediatR;
using FlowSynx.Application.Wrapper;

namespace FlowSynx.Application.Features.Plugins.Command.Uninstall;

public class UninstallPluginRequest : IRequest<Result<Unit>>
{
    public required string Type { get; set; }
    public required string Version { get; set; }
}