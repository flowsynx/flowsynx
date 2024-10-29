using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.IO.Compression;

namespace FlowSynx.Core.Features.Compress.Command;

public class CompressRequest : BaseRequest, IRequest<Result<CompressResult>>
{
    public string? CompressType { get; set; } = IO.Compression.CompressType.Zip.ToString();
}