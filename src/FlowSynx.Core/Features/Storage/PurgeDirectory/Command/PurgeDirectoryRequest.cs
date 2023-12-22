using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Storage.PurgeDirectory.Command;

public class PurgeDirectoryRequest : IRequest<Result<PurgeDirectoryResponse>>
{
    public required string Path { get; set; }
}