using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Genomes.Requests.GenomeChromosomeList;

internal class GenomeChromosomeListHandler : IActionHandler<GenomeChromosomeListRequest, PaginatedResult<GenomeChromosomeListResult>>
{
    private readonly ILogger<GenomeChromosomeListHandler> _logger;
    private readonly IChromosomeRepository _chromosomeRepository;
    private readonly ICurrentUserService _currentUserService;

    public GenomeChromosomeListHandler(
        ILogger<GenomeChromosomeListHandler> logger, 
        IChromosomeRepository chromosomeRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chromosomeRepository = chromosomeRepository ?? throw new ArgumentNullException(nameof(chromosomeRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<PaginatedResult<GenomeChromosomeListResult>> Handle(GenomeChromosomeListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var chromosomeExists = await _chromosomeRepository.Exist(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.GenomeId,
                cancellationToken);

            if (!chromosomeExists)
            {
                _logger.LogWarning("No genome found with GenomeId '{GenomeId}'.", request.GenomeId);
                return await PaginatedResult<GenomeChromosomeListResult>.FailureAsync($"No genome found with GenomeId {request.GenomeId}.");
            }

            var chromosomeGenes = await _chromosomeRepository.GetChromosomeGenesAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.GenomeId,
                cancellationToken);

            var response = chromosomeGenes.Select(chromosome => new GenomeChromosomeListResult
            {
                Name = chromosome.GeneId
            });

            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);

            _logger.LogInformation(
                "Genome chromosomes list retrieved successfully for page {Page} with size {PageSize}.",
                page,
                pageSize);
            return await PaginatedResult<GenomeChromosomeListResult>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in GenomeChromosomeListHandler for page {Page} with size {PageSize}.",
                request.Page,
                request.PageSize);
            return await PaginatedResult<GenomeChromosomeListResult>.FailureAsync(ex.Message);
        }
    }
}
