using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Config.Query.List;

public class ConfigListRequest : IRequest<Result<IEnumerable<ConfigListResponse>>>
{
    public string? Type { get; set; }
}