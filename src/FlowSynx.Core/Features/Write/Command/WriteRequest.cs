using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Core.Features.Write.Command;

public class WriteRequest : IRequest<Result<object>>
{
    public required string Entity { get; set; }
    public required object Data { get; set; }
    public PluginFilters? Filters { get; set; } = new PluginFilters();
}