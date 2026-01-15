using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Genes.Requests.GeneDetails;

internal class GeneDetailsHandler : IActionHandler<GeneDetailsRequest, Result<GeneDetailsResult>>
{
    private readonly ILogger<GeneDetailsHandler> _logger;
    private readonly IGeneRepository _geneRepository;
    private readonly ICurrentUserService _currentUserService;

    public GeneDetailsHandler(
        ILogger<GeneDetailsHandler> logger, 
        IGeneRepository geneRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _geneRepository = geneRepository ?? throw new ArgumentNullException(nameof(geneRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<GeneDetailsResult>> Handle(GeneDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var gene = await _geneRepository.GetByIdAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.Id,
                cancellationToken);

            var response = new GeneDetailsResult
            {
                Name = gene.Name,
                Namespace = gene.Namespace,
                Version = gene.Version,
                Description = gene.Description,
                Specification = gene.Specification,
                Metadata = gene.Metadata,
                Labels = gene.Labels,
                Annotations = gene.Annotations,
                Owner = gene.Owner,
                Status = gene.Status.ToString().ToLower(),
                IsShared = gene.IsShared
            };

            _logger.LogInformation(
                "Gene details retrieved successfully for gene {GeneId}.",
                request.Id);

            return await Result<GeneDetailsResult>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in GeneDetailsHandler for gene {GeneId}.",
                request.Id);
            return await Result<GeneDetailsResult>.FailAsync(ex.Message);
        }
    }
}
