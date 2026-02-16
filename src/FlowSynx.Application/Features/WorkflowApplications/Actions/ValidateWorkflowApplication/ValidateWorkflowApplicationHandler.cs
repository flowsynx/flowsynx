using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.WorkflowApplications.Actions.ValidateWorkflowApplication;

internal class ValidateWorkflowApplicationHandler : IActionHandler<ValidateWorkflowApplicationRequest, Result<ValidationResponse>>
{
    private readonly ILogger<ValidateWorkflowApplicationHandler> _logger;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;
    private readonly IWorkflowApplicationManagementService _workflowApplicationManagementService;
    private readonly ICurrentUserService _currentUserService;

    public ValidateWorkflowApplicationHandler(
        ILogger<ValidateWorkflowApplicationHandler> logger, 
        ISerializer serializer,
        IDeserializer deserializer,
        IWorkflowApplicationManagementService workflowApplicationManagementService,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        _workflowApplicationManagementService = workflowApplicationManagementService ?? throw new ArgumentNullException(nameof(workflowApplicationManagementService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<ValidationResponse>> Handle(ValidateWorkflowApplicationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var jsonString = _serializer.Serialize(request.Json);

            var result = await _workflowApplicationManagementService.ValidateJsonAsync(
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