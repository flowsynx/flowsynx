using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Plugins.Query.PluginDetails;

public class PluginDetailsRequest : IRequest<Result<PluginDetailsResponse>>
{
    public required string PluginId { get; set; }
}