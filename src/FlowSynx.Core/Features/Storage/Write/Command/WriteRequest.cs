using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Storage.Write.Command;

public class WriteRequest : IRequest<Result<WriteResponse>>
{
    public required string Path { get; set; }
    public required string Data { get; set; }
    public bool Overwrite { get; set; } = false;
}