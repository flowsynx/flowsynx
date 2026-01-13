using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.AuditTrails.Requests.AuditTrailDetails;

public class AuditTrailDetailsRequest : IAction<Result<AuditTrailDetailsResult>>
{
    public required long AuditId { get; set; }
}