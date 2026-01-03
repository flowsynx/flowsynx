using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Domain.Primitives;

namespace FlowSynx.Application.Features.AuditTrails.Requests.AuditTrailDetails;

public class AuditTrailDetailsRequest : IAction<Result<AuditTrailDetailsResult>>
{
    public required long AuditId { get; set; }
}