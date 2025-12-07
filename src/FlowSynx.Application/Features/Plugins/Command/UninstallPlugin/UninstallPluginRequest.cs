using MediatR;
using FlowSynx.Domain.Wrapper;

namespace FlowSynx.Application.Features.Plugins.Command.UninstallPlugin;

public class UninstallPluginRequest : IRequest<Result<Unit>>
{
    public required string Type { get; set; }
}