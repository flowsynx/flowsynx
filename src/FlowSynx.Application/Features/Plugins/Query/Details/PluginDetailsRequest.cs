using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Plugins.Query.Details;

public class PluginDetailsRequest : IRequest<Result<PluginDetailsResponse>>
{
    public required string Type { get; set; }
}