using FlowSynx.Abstractions;
using MediatR;

namespace FlowSynx.Core.Features.Config.Query.Details;

public class ConfigDetailsRequest : IRequest<Result<ConfigDetailsResponse>>
{
    public required string Name { get; set; }
}