using MediatR;
using FlowSync.Core.Common.Models;

namespace FlowSync.Core.Features.Storage.PurgeDirectory.Command;

public class PurgeDirectoryRequest : IRequest<Result<PurgeDirectoryResponse>>
{
    public required string Path { get; set; }
}