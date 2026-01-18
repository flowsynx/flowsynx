using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Chromosomes.Requests.ChromosomeGenesList;

internal class ChromosomeGenesListHandler : IActionHandler<ChromosomeGenesListRequest, PaginatedResult<ChromosomeGenesListResult>>
{
    private readonly ILogger<ChromosomeGenesListHandler> _logger;
    private readonly IChromosomeRepository _chromosomeRepository;
    private readonly ICurrentUserService _currentUserService;

    public ChromosomeGenesListHandler(
        ILogger<ChromosomeGenesListHandler> logger, 
        IChromosomeRepository chromosomeRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chromosomeRepository = chromosomeRepository ?? throw new ArgumentNullException(nameof(chromosomeRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<PaginatedResult<ChromosomeGenesListResult>> Handle(ChromosomeGenesListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var chromosomeExists = await _chromosomeRepository.Exist(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.ChromosomeId,
                cancellationToken);

            if (!chromosomeExists)
            {
                _logger.LogWarning("No chromosome found with ChromosomeId '{ChromosomeId}'.", request.ChromosomeId);
                return await PaginatedResult<ChromosomeGenesListResult>.FailureAsync($"No chromosome found with ChromosomeId {request.ChromosomeId}.");
            }

            var chromosomeGenes = await _chromosomeRepository.GetChromosomeGenesAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.ChromosomeId,
                cancellationToken);

            var response = chromosomeGenes.Select(chromosome => new ChromosomeGenesListResult
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
                "ChromosomeGenes list retrieved successfully for page {Page} with size {PageSize}.",
                page,
                pageSize);
            return await PaginatedResult<ChromosomeGenesListResult>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in ChromosomeGenesListHandler for page {Page} with size {PageSize}.",
                request.Page,
                request.PageSize);
            return await PaginatedResult<ChromosomeGenesListResult>.FailureAsync(ex.Message);
        }
    }
}
