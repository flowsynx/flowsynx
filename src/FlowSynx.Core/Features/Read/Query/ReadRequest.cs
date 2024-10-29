using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Features.Read.Query;

public class ReadRequest : BaseRequest, IRequest<Result<ReadResult>>
{

}