using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Delete.Command;

public class DeleteRequest : BaseRequest, IRequest<Result<Unit>>
{

}