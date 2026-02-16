using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.WorkflowApplications.Actions.ExecuteWorkflowApplication;

internal class ExecuteWorkflowApplicationHandler : IActionHandler<ExecuteWorkflowApplicationRequest, Result<ExecutionResponse>>
{
    private readonly ILogger<ExecuteWorkflowApplicationHandler> _logger;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;
    private readonly IWorkflowApplicationExecutionService _workflowApplicationExecutionService;
    private readonly ICurrentUserService _currentUserService;

    public ExecuteWorkflowApplicationHandler(
        ILogger<ExecuteWorkflowApplicationHandler> logger, 
        ISerializer serializer,
        IDeserializer deserializer,
        IWorkflowApplicationExecutionService workflowApplicationExecutionService,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        _workflowApplicationExecutionService = workflowApplicationExecutionService ?? throw new ArgumentNullException(nameof(workflowApplicationExecutionService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<ExecutionResponse>> Handle(ExecuteWorkflowApplicationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var jsonString = _serializer.Serialize(request.Json);
            var deserializedJson = _deserializer.Deserialize<ExecuteWorkflowApplicationRequestDefinition>(jsonString);

            var result = await _workflowApplicationExecutionService.ExecuteWorkflowApplicationAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.WorkflowApplicationId,
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