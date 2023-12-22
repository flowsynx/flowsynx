using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Plugins.Query.Details;

public class PluginDetailsRequest : IRequest<Result<PluginDetailsResponse>>
{
    public required Guid Id { get; set; }
}