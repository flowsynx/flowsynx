using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Features.Write.Command;

public class WriteRequest : BaseRequest, IRequest<Result<Unit>>
{
    public required object Data { get; set; }
}