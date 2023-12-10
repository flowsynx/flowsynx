using MediatR;
using FlowSync.Core.Common.Models;

namespace FlowSync.Core.Features.Storage.MakeDirectory.Command;

public class MakeDirectoryRequest : IRequest<Result<MakeDirectoryResponse>>
{
    public required string Path { get; set; }
}