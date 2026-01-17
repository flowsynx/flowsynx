using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Features.AuditTrails.Requests.AuditTrailDetails;
using FlowSynx.Application.Features.AuditTrails.Requests.AuditTrailsList;
using FlowSynx.Application.Features.Chromosomes.Actions.RegisterChromosome;
using FlowSynx.Application.Features.Chromosomes.Requests.ChromosomesList;
using FlowSynx.Application.Features.Execute;
using FlowSynx.Application.Features.Genes.Actions.DeleteGene;
using FlowSynx.Application.Features.Genes.Actions.ExecuteGene;
using FlowSynx.Application.Features.Genes.Actions.RegisterGene;
using FlowSynx.Application.Features.Genes.Actions.ValidateGene;
using FlowSynx.Application.Features.Genes.Requests.GeneDetails;
using FlowSynx.Application.Features.Genes.Requests.GenesList;
using FlowSynx.Application.Features.Tenants.Actions.AddTenant;
using FlowSynx.Application.Features.Tenants.Actions.DeleteTenant;
using FlowSynx.Application.Features.Tenants.Actions.UpdateTenant;
using FlowSynx.Application.Features.Tenants.Requests.TenantsDetails;
using FlowSynx.Application.Features.Tenants.Requests.TenantsList;
using FlowSynx.Application.Features.Version.VersionRequest;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;
using Void = FlowSynx.Application.Core.Dispatcher.Void;

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

    #region Genes
    public static Task<PaginatedResult<GenesListResult>> GenesList(
        this IDispatcher dispatcher,
        int page,
        int pageSize,
        string? @namespace,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new GenesListRequest
        {
            Namespace = @namespace,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<Result<GeneDetailsResult>> GeneDetails(
        this IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new GeneDetailsRequest { Id = id }, cancellationToken);
    }

    public static Task<Result<RegisterGeneResult>> RegisterGene(
        this IDispatcher dispatcher,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new RegisterGeneRequest { Json = json }, cancellationToken);
    }

    public static Task<Result<Void>> DeleteGene(
        this IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new DeleteGeneRequest { Id = id }, cancellationToken);
    }

    public static Task<Result<ExecutionResponse>> ExecuteGene(
        this IDispatcher dispatcher,
        Guid id,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ExecuteGeneRequest
        {
            GeneId = id,
            Json = json
        }, cancellationToken);
    }

    public static Task<Result<ValidationResponse>> ValidateGene(
        this IDispatcher dispatcher,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ValidateGeneRequest
        {
            Json = json
        }, cancellationToken);
    }
    #endregion

    #region Chromosomes
    public static Task<PaginatedResult<ChromosomesListResult>> ChromosomesList(
        this IDispatcher dispatcher,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ChromosomesListRequest
        {
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<Result<RegisterChromosomeResult>> RegisterChromosome(
        this IDispatcher dispatcher,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new RegisterChromosomeRequest { Json = json }, cancellationToken);
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

    #region Tenants
    public static Task<PaginatedResult<TenantsListResult>> Tenants(
        this IDispatcher dispatcher,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new TenantsListRequest
        {
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<Result<TenantDetailsResult>> TenantDetails(
        this IDispatcher dispatcher,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new TenantDetailsRequest { TenantId = tenantId }, cancellationToken);
    }

    public static Task<Result<AddTenantResult>> AddTenant(
        this IDispatcher dispatcher,
        AddTenantRequest tenantRequest,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(tenantRequest, cancellationToken);
    }

    public static Task<Result<Void>> UpdateTenant(
        this IDispatcher dispatcher,
        Guid tenantId,
        UpdateTenantDefinitionRequest updateTenantRequest,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new UpdateTenantRequest
        {
            TenantId = tenantId,
            Name = updateTenantRequest.Name,
            Description = updateTenantRequest.Description,
            Status = updateTenantRequest.Status
        }, cancellationToken);
    }

    public static Task<Result<DeleteTenantResult>> DeleteTenant(
        this IDispatcher dispatcher,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new DeleteTenantRequest { tenantId = tenantId }, cancellationToken);
    }
    #endregion
}