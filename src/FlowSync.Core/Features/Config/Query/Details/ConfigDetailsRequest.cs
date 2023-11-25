using MediatR;
using FlowSync.Core.Common.Models;

namespace FlowSync.Core.Features.Config.Query.Details;

public class ConfigDetailsRequest : IRequest<Result<ConfigDetailsResponse>>
{
    public required string Name { get; set; }
}