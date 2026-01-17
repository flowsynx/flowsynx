using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Chromosomes.Actions.ExecuteChromosome;

internal class ExecuteChromosomeHandler : IActionHandler<ExecuteChromosomeRequest, Result<ExecutionResponse>>
{
    private readonly ILogger<ExecuteChromosomeHandler> _logger;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;
    private readonly IGenomeExecutionService _genomeExecutionService;
    private readonly ICurrentUserService _currentUserService;

    public ExecuteChromosomeHandler(
        ILogger<ExecuteChromosomeHandler> logger, 
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

    public async Task<Result<ExecutionResponse>> Handle(ExecuteChromosomeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var jsonString = _serializer.Serialize(request.Json);
            var deserializedJson = _deserializer.Deserialize<ExecuteChromosomeRequestDefinition>(jsonString);

            var result = await _genomeExecutionService.ExecuteChromosomeAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.ChromosomeId,
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