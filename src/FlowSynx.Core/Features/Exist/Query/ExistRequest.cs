using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Core.Features.Exist.Query;

public class ExistRequest : IRequest<Result<object>>
{
    public required string Entity { get; set; }
    public PluginFilters? Filters { get; set; } = new PluginFilters();
}