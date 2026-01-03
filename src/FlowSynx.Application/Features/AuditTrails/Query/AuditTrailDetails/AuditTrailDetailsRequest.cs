using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Domain.Primitives;

namespace FlowSynx.Application.Features.AuditTrails.Query.AuditTrailDetails;

public class AuditTrailDetailsRequest : IAction<Result<AuditTrailDetailsResponse>>
{
    public required long AuditId { get; set; }
}