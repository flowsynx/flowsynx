using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.GeneBlueprints.GeneBlueprintRegister;

internal class GeneRegisterHandler : IActionHandler<GeneRegisterRequest, Result<GeneRegisterResult>>
{
    private readonly ILogger<GeneRegisterHandler> _logger;
    private readonly ISerializer _serializer;
    private readonly IGenomeManagementService _managementService;
    private readonly ICurrentUserService _currentUserService;

    public GeneRegisterHandler(
        ILogger<GeneRegisterHandler> logger, 
        ISerializer serializer,
        IGenomeManagementService managementService,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<GeneRegisterResult>> Handle(GeneRegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var jsonString = _serializer.Serialize(request.Json);
            var geneBlueprint = await _managementService.RegisterGeneBlueprintAsync(_currentUserService.UserId(), jsonString);

            var response = new GeneRegisterResult
            {
                Status = "registered",
                Id = geneBlueprint.Id,
                Name = geneBlueprint.Name,
                Version = geneBlueprint.Version,
                Namespace = geneBlueprint.Namespace
            };

            return await Result<GeneRegisterResult>.SuccessAsync(response);
        }
        catch (ValidationException vex)
        {
            var errorMessages = vex.Errors
                .SelectMany(kvp => kvp.Value.Select(msg => $"{kvp.Key}: {msg}"))
                .ToList();
            return await Result<GeneRegisterResult>.FailAsync(errorMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return await Result<GeneRegisterResult>.FailAsync(ex.Message);
        }
    }
}