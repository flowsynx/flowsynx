using FlowSynx.Core.Wrapper;
using MediatR;

namespace FlowSynx.Core.Features.PluginConfig.Query.Details;

public class PluginConfigDetailsRequest : IRequest<Result<PluginConfigDetailsResponse>>
{
    public required string Id { get; set; }
}