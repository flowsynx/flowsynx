using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.IO.Compression;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Features.Compress.Command;

public class CompressRequest : IRequest<Result<CompressResult>>
{
    public required string Entity { get; set; }
    public ConnectorOptions? Options { get; set; } = new ConnectorOptions();
    public string? CompressType { get; set; } = IO.Compression.CompressType.Zip.ToString();
}