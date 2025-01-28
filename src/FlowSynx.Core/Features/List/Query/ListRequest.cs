using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Data;

namespace FlowSynx.Core.Features.List.Query;

public class ListRequest : BaseRequest, IRequest<Result<InterchangeData>>
{

}