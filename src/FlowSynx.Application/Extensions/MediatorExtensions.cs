using FlowSynx.Application.Features.AuditTrails.Query.AuditTrailDetails;
using FlowSynx.Application.Features.AuditTrails.Query.AuditTrailsList;
using FlowSynx.Application.Features.LogEntries.Query.LogEntriesList;
using FlowSynx.Application.Features.Version.Query;
using FlowSynx.Domain.Primitives;
using MediatR;

namespace FlowSynx.Application.Extensions;

public static class MediatorExtensions
{
    #region Version
    public static Task<Result<VersionResponse>> Version(
        this IMediator mediator,
        VersionRequest request,
        CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }
    #endregion

    #region LogsEntries
    public static Task<PaginatedResult<LogEntriesListResponse>> Logs(
        this IMediator mediator,
        int page,
        int pageSize,
        LogEntriesListRequestTdo request,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new LogEntriesListRequest
        {
            Level = request.Level,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Message = request.Message,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }
    #endregion

    #region AuditTrails
    public static Task<PaginatedResult<AuditTrailsListResponse>> AuditTrails(
        this IMediator mediator,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new AuditTrailsListRequest
        {
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<Result<AuditTrailDetailsResponse>> AuditDetails(
        this IMediator mediator,
        string auditId,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new AuditTrailDetailsRequest { AuditId = auditId }, cancellationToken);
    }
    #endregion
}
