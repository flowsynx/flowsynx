using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Genomes.Actions.CreateGenome;

internal class CreateGenomeHandler : IActionHandler<CreateGenomeRequest, Result<CreateGenomeResult>>
{
    private readonly ILogger<CreateGenomeHandler> _logger;
    private readonly ISerializer _serializer;
    private readonly IGenomeManagementService _managementService;
    private readonly ICurrentUserService _currentUserService;

    public CreateGenomeHandler(
        ILogger<CreateGenomeHandler> logger, 
        ISerializer serializer,
        IGenomeManagementService managementService,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<CreateGenomeResult>> Handle(CreateGenomeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var jsonString = _serializer.Serialize(request.Json);
            var genome = await _managementService.RegisterGenomeAsync(_currentUserService.UserId(), jsonString);

            var response = new CreateGenomeResult
            {
                Status = "created",
                Id = genome.Id,
                Name = genome.Name,
                Namespace = genome.Namespace
            };

            return await Result<CreateGenomeResult>.SuccessAsync(response);
        }
        catch (ValidationException vex)
        {
            var errorMessages = vex.Errors
                .SelectMany(kvp => kvp.Value.Select(msg => $"{kvp.Key}: {msg}"))
                .ToList();
            return await Result<CreateGenomeResult>.FailAsync(errorMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return await Result<CreateGenomeResult>.FailAsync(ex.Message);
        }
    }
}