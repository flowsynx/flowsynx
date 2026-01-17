using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;
using Void = FlowSynx.Application.Core.Dispatcher.Void;

namespace FlowSynx.Application.Features.Chromosomes.Actions.DeleteChromosome;

internal class DeleteChromosomeHandler : IActionHandler<DeleteChromosomeRequest, Result<Void>>
{
    private readonly ILogger<DeleteChromosomeHandler> _logger;
    private readonly ISerializer _serializer;
    private readonly IChromosomeRepository _chromosomeRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteChromosomeHandler(
        ILogger<DeleteChromosomeHandler> logger, 
        ISerializer serializer,
        IChromosomeRepository chromosomeRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _chromosomeRepository = chromosomeRepository ?? throw new ArgumentNullException(nameof(chromosomeRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<Void>> Handle(DeleteChromosomeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            await _chromosomeRepository.DeleteAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(), request.Id, cancellationToken);

            return await Result<Void>.SuccessAsync(ApplicationResources.Feature_Chromosome_DeletedSuccessfully);
        }
        catch (ValidationException vex)
        {
            var errorMessages = vex.Errors
                .SelectMany(kvp => kvp.Value.Select(msg => $"{kvp.Key}: {msg}"))
                .ToList();
            return await Result<Void>.FailAsync(errorMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return await Result<Void>.FailAsync(ex.Message);
        }
    }
}