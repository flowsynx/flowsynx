using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.About.Query;

public class AboutRequest : BaseRequest, IRequest<Result<object>>
{

}