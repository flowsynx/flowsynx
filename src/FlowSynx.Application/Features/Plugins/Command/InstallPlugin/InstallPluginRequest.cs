using MediatR;
using FlowSynx.Domain.Wrapper;

namespace FlowSynx.Application.Features.Plugins.Command.InstallPlugin;

public class InstallPluginRequest : IRequest<Result<Unit>>
{
    public required string Type { get; set; }
}