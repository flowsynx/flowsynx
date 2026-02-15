using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Features.Activities.Actions.CreateActivity;
using FlowSynx.Application.Features.Activities.Actions.DeleteActivity;
using FlowSynx.Application.Features.Activities.Actions.ExecuteActivity;
using FlowSynx.Application.Features.Activities.Actions.ValidateActivity;
using FlowSynx.Application.Features.Activities.Requests.ActivitiesList;
using FlowSynx.Application.Features.Activities.Requests.ActivityDetails;
using FlowSynx.Application.Features.Activities.Requests.ActivityExecutionsList;
using FlowSynx.Application.Features.AuditTrails.Requests.AuditTrailDetails;
using FlowSynx.Application.Features.AuditTrails.Requests.AuditTrailsList;
using FlowSynx.Application.Features.Chromosome.Actions.CreateChromosome;
using FlowSynx.Application.Features.Chromosomes.Actions.DeleteChromosome;
using FlowSynx.Application.Features.Chromosomes.Actions.ExecuteChromosome;
using FlowSynx.Application.Features.Chromosomes.Actions.ValidateChromosome;
using FlowSynx.Application.Features.Chromosomes.Requests.ChromosomeDetails;
using FlowSynx.Application.Features.Chromosomes.Requests.ChromosomeExecutionsList;
using FlowSynx.Application.Features.Chromosomes.Requests.ChromosomeGenesList;
using FlowSynx.Application.Features.Chromosomes.Requests.ChromosomesList;
using FlowSynx.Application.Features.Execute;
using FlowSynx.Application.Features.Genomes.Actions.CreateGenome;
using FlowSynx.Application.Features.Genomes.Actions.DeleteGenome;
using FlowSynx.Application.Features.Genomes.Actions.ExecuteGenome;
using FlowSynx.Application.Features.Genomes.Actions.ValidateGenome;
using FlowSynx.Application.Features.Genomes.Requests.GenomeChromosomeList;
using FlowSynx.Application.Features.Genomes.Requests.GenomeDetails;
using FlowSynx.Application.Features.Genomes.Requests.GenomeExecutionsList;
using FlowSynx.Application.Features.Genomes.Requests.GenomesList;
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

    #region Activities
    public static Task<PaginatedResult<ActivitiesListResult>> ActivitiesList(
        this IDispatcher dispatcher,
        int page,
        int pageSize,
        string? @namespace,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ActivitiesListRequest
        {
            Namespace = @namespace,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<Result<ActivityDetailsResult>> ActivityDetails(
        this IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ActivityDetailsRequest { Id = id }, cancellationToken);
    }

    public static Task<Result<CreateActivityResult>> CreateActivity(
        this IDispatcher dispatcher,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new CreateActivityRequest { Json = json }, cancellationToken);
    }

    public static Task<Result<Void>> DeleteActivity(
        this IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new DeleteActivityRequest { Id = id }, cancellationToken);
    }

    public static Task<Result<ExecutionResponse>> ExecuteActivity(
        this IDispatcher dispatcher,
        Guid id,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ExecuteActivityRequest
        {
            ActivityId = id,
            Json = json
        }, cancellationToken);
    }

    public static Task<Result<ValidationResponse>> ValidateActivity(
        this IDispatcher dispatcher,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ValidateActivityRequest
        {
            Json = json
        }, cancellationToken);
    }

    public static Task<PaginatedResult<ActivityExecutionsListResult>> ActivityExecutionHistoryList(
        this IDispatcher dispatcher,
        Guid activityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ActivityExecutionsListRequest
        {
            ActivityId = activityId,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }
    #endregion

    #region Genomes
    public static Task<PaginatedResult<GenomesListResult>> GenomesList(
        this IDispatcher dispatcher,
        int page,
        int pageSize,
        string? @namespace,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new GenomesListRequest
        {
            Namespace = @namespace,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<Result<GenomeDetailsResult>> GenomeDetails(
        this IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new GenomeDetailsRequest { Id = id }, cancellationToken);
    }

    public static Task<Result<CreateGenomeResult>> CreateGenome(
        this IDispatcher dispatcher,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new CreateGenomeRequest { Json = json }, cancellationToken);
    }

    public static Task<Result<Void>> DeleteGenome(
        this IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new DeleteGenomeRequest { Id = id }, cancellationToken);
    }

    public static Task<Result<ExecutionResponse>> ExecuteGenome(
        this IDispatcher dispatcher,
        Guid id,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ExecuteGenomeRequest
        {
            GenomeId = id,
            Json = json
        }, cancellationToken);
    }

    public static Task<Result<ValidationResponse>> ValidateGenome(
        this IDispatcher dispatcher,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ValidateGenomeRequest
        {
            Json = json
        }, cancellationToken);
    }

    public static Task<PaginatedResult<GenomeExecutionsListResult>> GenomeExecutionHistoryList(
        this IDispatcher dispatcher,
        Guid chromosomeId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new GenomeExecutionsListRequest
        {
            GenomeId = chromosomeId,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<PaginatedResult<GenomeChromosomeListResult>> GenomeChromosomesList(
        this IDispatcher dispatcher,
        Guid genomeId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new GenomeChromosomeListRequest
        {
            GenomeId = genomeId,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }
    #endregion

    #region Chromosomes
    public static Task<PaginatedResult<ChromosomesListResult>> ChromosomesList(
        this IDispatcher dispatcher,
        int page,
        int pageSize,
        string? @namespace,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ChromosomesListRequest
        {
            Namespace = @namespace,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<Result<ChromosomeDetailsResult>> ChromosomeDetails(
        this IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ChromosomeDetailsRequest { Id = id }, cancellationToken);
    }

    public static Task<Result<CreateChromosomeResult>> CreateChromosome(
        this IDispatcher dispatcher,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new CreateChromosomeRequest { Json = json }, cancellationToken);
    }

    public static Task<Result<Void>> DeleteChromosome(
        this IDispatcher dispatcher,
        Guid id,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new DeleteChromosomeRequest { Id = id }, cancellationToken);
    }

    public static Task<Result<ExecutionResponse>> ExecuteChromosome(
        this IDispatcher dispatcher,
        Guid id,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ExecuteChromosomeRequest
        {
            ChromosomeId = id,
            Json = json
        }, cancellationToken);
    }

    public static Task<Result<ValidationResponse>> ValidateChromosome(
        this IDispatcher dispatcher,
        object json,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ValidateChromosomeRequest
        {
            Json = json
        }, cancellationToken);
    }

    public static Task<PaginatedResult<ChromosomeExecutionsListResult>> ChromosomeExecutionHistoryList(
        this IDispatcher dispatcher,
        Guid chromosomeId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ChromosomeExecutionsListRequest
        {
            ChromosomeId = chromosomeId,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
    }

    public static Task<PaginatedResult<ChromosomeGenesListResult>> ChromosomeGenesList(
        this IDispatcher dispatcher,
        Guid chromosomeId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return dispatcher.Dispatch(new ChromosomeGenesListRequest
        {
            ChromosomeId = chromosomeId,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
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