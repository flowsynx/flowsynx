using MediatR;
using FlowSynx.Domain.Wrapper;

namespace FlowSynx.Application.Features.PluginConfig.Command.DeletePluginConfig;

public class DeletePluginConfigRequest : IRequest<Result<Unit>>
{
    public required string ConfigId { get; set; }
}