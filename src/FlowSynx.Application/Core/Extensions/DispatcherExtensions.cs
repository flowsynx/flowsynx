using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Features.AuditTrails.Requests.AuditTrailDetails;
using FlowSynx.Application.Features.AuditTrails.Requests.AuditTrailsList;
using FlowSynx.Application.Features.Execute;
using FlowSynx.Application.Features.GeneBlueprints.GeneBlueprintRegister;
using FlowSynx.Application.Features.Version.VersionRequest;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Core.Extensions;

public static class DispatcherExtensions
{
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

    #region Execute
    public static Task<Result<ExecutionResponse>> Execute(
        this IDispatcher dispatcher,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ExecuteRequest { Json = json }, cancellationToken);
    }
    #endregion

    #region GeneBlueprints
    public static Task<Result<GeneRegisterResult>> RegisterGene(
        this IDispatcher dispatcher,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new GeneRegisterRequest { Json = json }, cancellationToken);
    }
    #endregion

    #region Version
    public static Task<Result<VersionResult>> Version(
        this IDispatcher dispatcher,
        VersionRequest request,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(request, cancellationToken);
    }
    #endregion

    
}
