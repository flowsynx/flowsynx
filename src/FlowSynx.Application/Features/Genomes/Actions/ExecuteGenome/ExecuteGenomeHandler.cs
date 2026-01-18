using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Genomes.Actions.ExecuteGenome;

internal class ExecuteGenomeHandler : IActionHandler<ExecuteGenomeRequest, Result<ExecutionResponse>>
{
    private readonly ILogger<ExecuteGenomeHandler> _logger;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;
    private readonly IGenomeExecutionService _genomeExecutionService;
    private readonly ICurrentUserService _currentUserService;

    public ExecuteGenomeHandler(
        ILogger<ExecuteGenomeHandler> logger, 
        ISerializer serializer,
        IDeserializer deserializer,
        IGenomeExecutionService genomeExecutionService,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        _genomeExecutionService = genomeExecutionService ?? throw new ArgumentNullException(nameof(genomeExecutionService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<ExecutionResponse>> Handle(ExecuteGenomeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var jsonString = _serializer.Serialize(request.Json);
            var deserializedJson = _deserializer.Deserialize<ExecuteGenomeRequestDefinition>(jsonString);

            var result = await _genomeExecutionService.ExecuteGenomeAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.GenomeId,
                deserializedJson.Context ?? new Dictionary<string, object>(),
                cancellationToken);

            return await Result<ExecutionResponse>.SuccessAsync(result);
        }
        catch (ValidationException vex)
        {
            var errorMessages = vex.Errors
                .SelectMany(kvp => kvp.Value.Select(msg => $"{kvp.Key}: {msg}"))
                .ToList();
            return await Result<ExecutionResponse>.FailAsync(errorMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return await Result<ExecutionResponse>.FailAsync(ex.Message);
        }
    }
}