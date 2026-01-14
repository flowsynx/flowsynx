using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.GeneBlueprints.Requests.GeneblueprintsList;

internal class GeneblueprintsListHandler : IActionHandler<GeneblueprintsListRequest, PaginatedResult<GeneblueprintsListResult>>
{
    private readonly ILogger<GeneblueprintsListHandler> _logger;
    private readonly IGeneBlueprintRepository _geneBlueprintRepository;
    private readonly ICurrentUserService _currentUserService;

    public GeneblueprintsListHandler(
        ILogger<GeneblueprintsListHandler> logger, 
        IGeneBlueprintRepository geneBlueprintRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _geneBlueprintRepository = geneBlueprintRepository ?? throw new ArgumentNullException(nameof(geneBlueprintRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<PaginatedResult<GeneblueprintsListResult>> Handle(GeneblueprintsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var geneBlueprints = await _geneBlueprintRepository.GetAllAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                cancellationToken);
            var response = geneBlueprints.Select(geneBlueprint => new GeneblueprintsListResult
            {
                Name = geneBlueprint.Name,
                Namespace = geneBlueprint.Namespace,
                Version = geneBlueprint.Version,
                Description = geneBlueprint.Description,
                Labels = geneBlueprint.Labels,
                Annotations = geneBlueprint.Annotations,
                Owner = geneBlueprint.Owner,
                Status = geneBlueprint.Status.ToString().ToLower(),
                IsShared = geneBlueprint.IsShared
            });

            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);

            _logger.LogInformation(
                "GeneBlueprint list retrieved successfully for page {Page} with size {PageSize}.",
                page,
                pageSize);
            return await PaginatedResult<GeneblueprintsListResult>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in GeneBlueprintsListHandler for page {Page} with size {PageSize}.",
                request.Page,
                request.PageSize);
            return await PaginatedResult<GeneblueprintsListResult>.FailureAsync(ex.Message);
        }
    }
}
