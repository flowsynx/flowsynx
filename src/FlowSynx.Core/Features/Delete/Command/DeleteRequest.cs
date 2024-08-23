using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Core.Features.Delete.Command;

public class DeleteRequest : IRequest<Result<IEnumerable<object>>>
{
    public required string Entity { get; set; }
    public PluginFilters? Filters { get; set; } = new PluginFilters();
}