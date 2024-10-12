using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.IO.Compression;

namespace FlowSynx.Core.Features.Compress.Command;

public class CompressRequest : IRequest<Result<CompressResult>>
{
    public required string Entity { get; set; }
    public FlowSynx.Connectors.Abstractions.Options? Options { get; set; } = new FlowSynx.Connectors.Abstractions.Options();
    public string? CompressType { get; set; } = IO.Compression.CompressType.Zip.ToString();
}