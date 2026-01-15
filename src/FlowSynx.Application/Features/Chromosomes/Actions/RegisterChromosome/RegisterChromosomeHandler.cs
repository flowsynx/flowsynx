using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Chromosomes.Actions.RegisterChromosome;

internal class RegisterChromosomeHandler : IActionHandler<RegisterChromosomeRequest, Result<RegisterChromosomeResult>>
{
    private readonly ILogger<RegisterChromosomeHandler> _logger;
    private readonly ISerializer _serializer;
    private readonly IGenomeManagementService _managementService;
    private readonly ICurrentUserService _currentUserService;

    public RegisterChromosomeHandler(
        ILogger<RegisterChromosomeHandler> logger, 
        ISerializer serializer,
        IGenomeManagementService managementService,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<RegisterChromosomeResult>> Handle(RegisterChromosomeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var jsonString = _serializer.Serialize(request.Json);
            var geneBlueprint = await _managementService.RegisterChromosomeAsync(_currentUserService.UserId(), jsonString);

            var response = new RegisterChromosomeResult
            {
                Status = "registered",
                Id = geneBlueprint.Id,
                Name = geneBlueprint.Name,
                Namespace = geneBlueprint.Namespace
            };

            return await Result<RegisterChromosomeResult>.SuccessAsync(response);
        }
        catch (ValidationException vex)
        {
            var errorMessages = vex.Errors
                .SelectMany(kvp => kvp.Value.Select(msg => $"{kvp.Key}: {msg}"))
                .ToList();
            return await Result<RegisterChromosomeResult>.FailAsync(errorMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return await Result<RegisterChromosomeResult>.FailAsync(ex.Message);
        }
    }
}