using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Genes.Actions.RegisterGene;

internal class RegisterGeneHandler : IActionHandler<RegisterGeneRequest, Result<RegisterGeneResult>>
{
    private readonly ILogger<RegisterGeneHandler> _logger;
    private readonly ISerializer _serializer;
    private readonly IGenomeManagementService _managementService;
    private readonly ICurrentUserService _currentUserService;

    public RegisterGeneHandler(
        ILogger<RegisterGeneHandler> logger, 
        ISerializer serializer,
        IGenomeManagementService managementService,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<RegisterGeneResult>> Handle(RegisterGeneRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var jsonString = _serializer.Serialize(request.Json);
            var gene = await _managementService.RegisterGeneAsync(_currentUserService.UserId(), jsonString);

            var response = new RegisterGeneResult
            {
                Status = "registered",
                Id = gene.Id,
                Name = gene.Name,
                Version = gene.Version,
                Namespace = gene.Namespace
            };

            return await Result<RegisterGeneResult>.SuccessAsync(response);
        }
        catch (ValidationException vex)
        {
            var errorMessages = vex.Errors
                .SelectMany(kvp => kvp.Value.Select(msg => $"{kvp.Key}: {msg}"))
                .ToList();
            return await Result<RegisterGeneResult>.FailAsync(errorMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return await Result<RegisterGeneResult>.FailAsync(ex.Message);
        }
    }
}