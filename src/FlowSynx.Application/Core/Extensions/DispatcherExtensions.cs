using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Results;
using FlowSynx.Application.Features.AuditTrails.Requests.AuditTrailDetails;
using FlowSynx.Application.Features.AuditTrails.Requests.AuditTrailsList;
using FlowSynx.Application.Features.Version.VersionRequest;

namespace FlowSynx.Application.Core.Extensions;

public static class DispatcherExtensions
{
    #region Version
    public static Task<Result<VersionResult>> Version(
        this IDispatcher dispatcher,
        VersionRequest request,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(request, cancellationToken);
    }
    #endregion

    #region AuditTrails
    public static Task<PaginatedResult<AuditTrailsListResult>> AuditTrails(
        this IDispatcher dispatcher,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new AuditTrailsListRequest
        {
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<Result<AuditTrailDetailsResult>> AuditDetails(
        this IDispatcher dispatcher,
        long auditId,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new AuditTrailDetailsRequest { AuditId = auditId }, cancellationToken);
    }
    #endregion
}
