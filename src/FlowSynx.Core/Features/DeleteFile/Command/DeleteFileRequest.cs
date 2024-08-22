using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.DeleteFile.Command;

public class DeleteFileRequest : IRequest<Result<DeleteFileResponse>>
{
    public required string Path { get; set; }
}