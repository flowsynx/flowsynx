using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Chromosomes.Requests.ChromosomesList;

internal class ChromosomesListHandler : IActionHandler<ChromosomesListRequest, PaginatedResult<ChromosomesListResult>>
{
    private readonly ILogger<ChromosomesListHandler> _logger;
    private readonly IChromosomeRepository _chromosomeRepository;
    private readonly ICurrentUserService _currentUserService;

    public ChromosomesListHandler(
        ILogger<ChromosomesListHandler> logger, 
        IChromosomeRepository chromosomeRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chromosomeRepository = chromosomeRepository ?? throw new ArgumentNullException(nameof(chromosomeRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<PaginatedResult<ChromosomesListResult>> Handle(ChromosomesListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var chromosomes = await _chromosomeRepository.GetByNamespaceAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.Namespace,
                cancellationToken);

            var response = chromosomes.Select(chromosome => new ChromosomesListResult
            {
                Name = chromosome.Name,
                Namespace = chromosome.Namespace,
                Description = chromosome.Description,
                Labels = chromosome.Labels,
                Annotations = chromosome.Annotations,
            });

            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);

            _logger.LogInformation(
                "Chromosomes list retrieved successfully for page {Page} with size {PageSize}.",
                page,
                pageSize);
            return await PaginatedResult<ChromosomesListResult>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in ChromosomesListHandler for page {Page} with size {PageSize}.",
                request.Page,
                request.PageSize);
            return await PaginatedResult<ChromosomesListResult>.FailureAsync(ex.Message);
        }
    }
}
