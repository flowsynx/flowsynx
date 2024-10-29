using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.List.Query;

public class ListRequest : BaseRequest, IRequest<Result<IEnumerable<object>>>
{

}