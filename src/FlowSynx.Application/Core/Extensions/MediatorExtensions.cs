using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Features.AuditTrails.Query.AuditTrailDetails;
using FlowSynx.Application.Features.AuditTrails.Query.AuditTrailsList;
using FlowSynx.Application.Features.Version.Inquiry;
using FlowSynx.Domain.Primitives;

namespace FlowSynx.Application.Core.Extensions;

public static class MediatorExtensions
{
    #region Version
    public static Task<Result<VersionResult>> Version(
        this IDispatcher dispatcher,
        VersionInquiry inquiry,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(inquiry, cancellationToken);
    }
    #endregion

    #region AuditTrails
    public static Task<PaginatedResult<AuditTrailsListResponse>> AuditTrails(
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

    public static Task<Result<AuditTrailDetailsResponse>> AuditDetails(
        this IDispatcher dispatcher,
        long auditId,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new AuditTrailDetailsRequest { AuditId = auditId }, cancellationToken);
    }
    #endregion
}
