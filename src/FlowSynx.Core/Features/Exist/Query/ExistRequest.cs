using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Exist.Query;

public class ExistRequest : BaseRequest, IRequest<Result<object>>
{

}