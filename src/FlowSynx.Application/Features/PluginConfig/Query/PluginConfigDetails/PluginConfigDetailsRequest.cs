using FlowSynx.Domain.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.PluginConfig.Query.PluginConfigDetails;

public class PluginConfigDetailsRequest : IRequest<Result<PluginConfigDetailsResponse>>
{
    public required string ConfigId { get; set; }
}