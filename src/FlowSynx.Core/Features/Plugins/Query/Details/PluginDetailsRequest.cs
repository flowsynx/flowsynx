using FlowSynx.Core.Wrapper;
using MediatR;

namespace FlowSynx.Core.Features.Plugins.Query.Details;

public class PluginDetailsRequest : IRequest<Result<PluginDetailsResponse>>
{
    public required string Type { get; set; }
}