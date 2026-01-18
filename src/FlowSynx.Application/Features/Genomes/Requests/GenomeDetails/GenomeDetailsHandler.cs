using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Genomes.Requests.GenomeDetails;

internal class GenomeDetailsHandler : IActionHandler<GenomeDetailsRequest, Result<GenomeDetailsResult>>
{
    private readonly ILogger<GenomeDetailsHandler> _logger;
    private readonly IGenomeRepository _genomeRepository;
    private readonly ICurrentUserService _currentUserService;

    public GenomeDetailsHandler(
        ILogger<GenomeDetailsHandler> logger, 
        IGenomeRepository genomeRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _genomeRepository = genomeRepository ?? throw new ArgumentNullException(nameof(genomeRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<GenomeDetailsResult>> Handle(GenomeDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var genome = await _genomeRepository.GetByIdAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.Id,
                cancellationToken);

            var response = new GenomeDetailsResult
            {
                Name = genome?.Name ?? string.Empty,
                Namespace = genome?.Namespace ?? string.Empty,
                Description = genome?.Description ?? string.Empty,
                Specification = genome?.Specification!,
                Metadata = genome?.Metadata ?? new Dictionary<string, object>(),
                Labels = genome?.Labels ?? new Dictionary<string, string>(),
                Annotations = genome?.Annotations ?? new Dictionary<string, string>()
            };

            _logger.LogInformation(
                "Genome details retrieved successfully for genome {GenomeId}.",
                request.Id);

            return await Result<GenomeDetailsResult>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in GenomeDetailsHandler for genome {GenomeId}.",
                request.Id);
            return await Result<GenomeDetailsResult>.FailAsync(ex.Message);
        }
    }
}
