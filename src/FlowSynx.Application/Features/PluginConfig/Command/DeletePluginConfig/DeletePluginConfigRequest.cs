using MediatR;
using FlowSynx.Application.Wrapper;

namespace FlowSynx.Application.Features.PluginConfig.Command.DeletePluginConfig;

public class DeletePluginConfigRequest : IRequest<Result<Unit>>
{
    public required string Id { get; set; }
}