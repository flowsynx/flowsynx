using FlowSynx.Domain.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Metrics.Query;

public class SummaryRequest : IRequest<Result<SummaryResponse>>
{

}