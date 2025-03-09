using MediatR;
using FlowSynx.Core.Wrapper;

namespace FlowSynx.Core.Features.PluginConfig.Command.Delete;

public class DeletePluginConfigRequest : IRequest<Result<Unit>>
{
    public string Name { get; set; } = string.Empty;
}