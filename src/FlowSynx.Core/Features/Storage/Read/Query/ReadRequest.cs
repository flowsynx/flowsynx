using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Storage.Read.Query;

public class ReadRequest : IRequest<Result<ReadResponse>>
{
    public required string Path { get; set; }
    public bool? Hashing { get; set; } = false;
}