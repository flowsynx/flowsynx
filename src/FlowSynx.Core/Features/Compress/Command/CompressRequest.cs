using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.IO.Compression;

namespace FlowSynx.Core.Features.Compress.Command;

public class CompressRequest : IRequest<Result<object>>
{
    public required string Entity { get; set; }
    public PluginFilters? Filters { get; set; } = new PluginFilters();
    public string? CompressType { get; set; } = IO.Compression.CompressType.Zip.ToString();
}