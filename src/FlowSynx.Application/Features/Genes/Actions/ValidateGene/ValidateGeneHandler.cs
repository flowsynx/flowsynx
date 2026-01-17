using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Genes.Actions.ValidateGene;

internal class ValidateGeneHandler : IActionHandler<ValidateGeneRequest, Result<ValidationResponse>>
{
    private readonly ILogger<ValidateGeneHandler> _logger;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;
    private readonly IGenomeManagementService _genomeManagementService;
    private readonly ICurrentUserService _currentUserService;

    public ValidateGeneHandler(
        ILogger<ValidateGeneHandler> logger, 
        ISerializer serializer,
        IDeserializer deserializer,
        IGenomeManagementService genomeManagementService,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        _genomeManagementService = genomeManagementService ?? throw new ArgumentNullException(nameof(genomeManagementService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<ValidationResponse>> Handle(ValidateGeneRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var jsonString = _serializer.Serialize(request.Json);

            var result = await _genomeManagementService.ValidateJsonAsync(
                _currentUserService.UserId(),
                jsonString,
                cancellationToken);

            return await Result<ValidationResponse>.SuccessAsync(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return await Result<ValidationResponse>.FailAsync(ex.Message);
        }
    }
}