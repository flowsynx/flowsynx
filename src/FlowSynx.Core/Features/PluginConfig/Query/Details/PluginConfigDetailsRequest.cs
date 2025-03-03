using FlowSynx.Core.Features.Config.Query.Details;
using FlowSynx.Core.Wrapper;
using MediatR;

namespace FlowSynx.Core.Features.PluginConfig.Query.List;

public class PluginConfigDetailsRequest : IRequest<Result<PluginConfigDetailsResponse>>
{
    public required string Name { get; set; }
}