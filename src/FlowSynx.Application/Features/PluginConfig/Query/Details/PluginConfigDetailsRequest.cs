using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.PluginConfig.Query.Details;

public class PluginConfigDetailsRequest : IRequest<Result<PluginConfigDetailsResponse>>
{
    public required string Id { get; set; }
}