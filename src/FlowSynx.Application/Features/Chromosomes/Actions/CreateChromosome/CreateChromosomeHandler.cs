using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Chromosome.Actions.CreateChromosome;

internal class CreateChromosomeHandler : IActionHandler<CreateChromosomeRequest, Result<CreateChromosomeResult>>
{
    private readonly ILogger<CreateChromosomeHandler> _logger;
    private readonly ISerializer _serializer;
    private readonly IGenomeManagementService _managementService;
    private readonly ICurrentUserService _currentUserService;

    public CreateChromosomeHandler(
        ILogger<CreateChromosomeHandler> logger, 
        ISerializer serializer,
        IGenomeManagementService managementService,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<CreateChromosomeResult>> Handle(CreateChromosomeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var jsonString = _serializer.Serialize(request.Json);
            var chromosome = await _managementService.RegisterChromosomeAsync(_currentUserService.UserId(), jsonString);

            var response = new CreateChromosomeResult
            {
                Status = "created",
                Id = chromosome.Id,
                Name = chromosome.Name,
                Namespace = chromosome.Namespace
            };

            return await Result<CreateChromosomeResult>.SuccessAsync(response);
        }
        catch (ValidationException vex)
        {
            var errorMessages = vex.Errors
                .SelectMany(kvp => kvp.Value.Select(msg => $"{kvp.Key}: {msg}"))
                .ToList();
            return await Result<CreateChromosomeResult>.FailAsync(errorMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return await Result<CreateChromosomeResult>.FailAsync(ex.Message);
        }
    }
}