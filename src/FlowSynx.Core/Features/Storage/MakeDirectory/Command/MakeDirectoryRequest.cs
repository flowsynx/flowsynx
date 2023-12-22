using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Storage.MakeDirectory.Command;

public class MakeDirectoryRequest : IRequest<Result<MakeDirectoryResponse>>
{
    public required string Path { get; set; }
}