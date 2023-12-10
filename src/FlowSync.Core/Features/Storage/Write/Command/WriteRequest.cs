using MediatR;
using FlowSync.Core.Common.Models;
using FlowSync.Abstractions.Storage;

namespace FlowSync.Core.Features.Storage.Write.Command;

public class WriteRequest : IRequest<Result<WriteResponse>>
{
    public required string Path { get; set; }
    public required string Data { get; set; }
}