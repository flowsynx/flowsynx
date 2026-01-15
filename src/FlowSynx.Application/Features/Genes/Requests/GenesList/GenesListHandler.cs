using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Genes.Requests.GenesList;

internal class GenesListHandler : IActionHandler<GenesListRequest, PaginatedResult<GenesListResult>>
{
    private readonly ILogger<GenesListHandler> _logger;
    private readonly IGeneRepository _geneRepository;
    private readonly ICurrentUserService _currentUserService;

    public GenesListHandler(
        ILogger<GenesListHandler> logger, 
        IGeneRepository geneRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _geneRepository = geneRepository ?? throw new ArgumentNullException(nameof(geneRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<PaginatedResult<GenesListResult>> Handle(GenesListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var genes = await _geneRepository.GetAllAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                cancellationToken);
            var response = genes.Select(gene => new GenesListResult
            {
                Name = gene.Name,
                Namespace = gene.Namespace,
                Version = gene.Version,
                Description = gene.Description,
                Labels = gene.Labels,
                Annotations = gene.Annotations,
                Owner = gene.Owner,
                Status = gene.Status.ToString().ToLower(),
                IsShared = gene.IsShared
            });

            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);

            _logger.LogInformation(
                "Genes list retrieved successfully for page {Page} with size {PageSize}.",
                page,
                pageSize);
            return await PaginatedResult<GenesListResult>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in GenesListHandler for page {Page} with size {PageSize}.",
                request.Page,
                request.PageSize);
            return await PaginatedResult<GenesListResult>.FailureAsync(ex.Message);
        }
    }
}
