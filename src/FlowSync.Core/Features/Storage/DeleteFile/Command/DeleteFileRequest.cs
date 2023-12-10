using MediatR;
using FlowSync.Core.Common.Models;

namespace FlowSync.Core.Features.Storage.DeleteFile.Command;

public class DeleteFileRequest : IRequest<Result<DeleteFileResponse>>
{
    public required string Path { get; set; }
}