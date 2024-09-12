using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.IO.Compression;
using FlowSynx.Plugin;

namespace FlowSynx.Core.Features.Compress.Command;

public class CompressRequest : IRequest<Result<CompressResult>>
{
    public required string Entity { get; set; }
    public PluginOptions? Options { get; set; } = new PluginOptions();
    public string? CompressType { get; set; } = IO.Compression.CompressType.Zip.ToString();
}