using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Write.Command;

public class WriteRequest : BaseRequest, IRequest<Result<Unit>>
{

}