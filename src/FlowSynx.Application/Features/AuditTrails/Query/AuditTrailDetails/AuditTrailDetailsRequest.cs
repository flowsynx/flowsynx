using FlowSynx.Domain.Primitives;
using MediatR;

namespace FlowSynx.Application.Features.AuditTrails.Query.AuditTrailDetails;

public class AuditTrailDetailsRequest : IRequest<Result<AuditTrailDetailsResponse>>
{
    public required long AuditId { get; set; }
}