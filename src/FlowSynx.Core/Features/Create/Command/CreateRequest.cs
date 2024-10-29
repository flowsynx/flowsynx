using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Create.Command;

public class CreateRequest : BaseRequest, IRequest<Result<Unit>>
{

}