using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Execute;

internal class ExecuteHandler : IActionHandler<ExecuteRequest, Result<ExecutionResponse>>
{
    private readonly ILogger<ExecuteHandler> _logger;
    private readonly ISerializer _serializer;
    private readonly IWorkflowApplicationManagementService _managementService;
    private readonly ICurrentUserService _currentUserService;

    public ExecuteHandler(
        ILogger<ExecuteHandler> logger,
        ISerializer serializer,
        IWorkflowApplicationManagementService managementService,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<ExecutionResponse>> Handle(ExecuteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var jsonString = _serializer.Serialize(request.Json);
            var result = await _managementService.ExecuteJsonAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(), jsonString);

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