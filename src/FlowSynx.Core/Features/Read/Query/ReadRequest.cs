using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Data;

namespace FlowSynx.Core.Features.Read.Query;

public class ReadRequest : BaseRequest, IRequest<Result<InterchangeData>>
{

}