using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Storage.DeleteFile.Command;

public class DeleteFileRequest : IRequest<Result<DeleteFileResponse>>
{
    public required string Path { get; set; }
}