using MediatR;
using FlowSync.Core.Common.Models;

namespace FlowSync.Core.Features.Storage.Read.Query;

public class ReadRequest : IRequest<Result<ReadResponse>>
{
    public required string Path { get; set; }
}