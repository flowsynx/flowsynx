using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Chromosomes.Requests.ChromosomeDetails;

internal class ChromosomeDetailsHandler : IActionHandler<ChromosomeDetailsRequest, Result<ChromosomeDetailsResult>>
{
    private readonly ILogger<ChromosomeDetailsHandler> _logger;
    private readonly IChromosomeRepository _chromosomeRepository;
    private readonly ICurrentUserService _currentUserService;

    public ChromosomeDetailsHandler(
        ILogger<ChromosomeDetailsHandler> logger, 
        IChromosomeRepository chromosomeRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chromosomeRepository = chromosomeRepository ?? throw new ArgumentNullException(nameof(chromosomeRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<ChromosomeDetailsResult>> Handle(ChromosomeDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var chromosome = await _chromosomeRepository.GetByIdAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.Id,
                cancellationToken);

            var response = new ChromosomeDetailsResult
            {
                Name = chromosome?.Name ?? string.Empty,
                Namespace = chromosome?.Namespace ?? string.Empty,
                Description = chromosome?.Description ?? string.Empty,
                Specification = chromosome?.Spec!,
                Metadata = chromosome?.Metadata ?? new Dictionary<string, object>(),
                Labels = chromosome?.Labels ?? new Dictionary<string, string>(),
                Annotations = chromosome?.Annotations ?? new Dictionary<string, string>()
            };

            _logger.LogInformation(
                "Chromosome details retrieved successfully for chromosome {ChromosomeId}.",
                request.Id);

            return await Result<ChromosomeDetailsResult>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in ChromosomeDetailsHandler for chromosome {ChromosomeId}.",
                request.Id);
            return await Result<ChromosomeDetailsResult>.FailAsync(ex.Message);
        }
    }
}
