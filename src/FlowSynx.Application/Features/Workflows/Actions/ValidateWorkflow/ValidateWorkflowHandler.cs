using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Workflows.Actions.ValidateWorkflow;

internal class ValidateWorkflowHandler : IActionHandler<ValidateWorkflowRequest, Result<ValidationResponse>>
{
    private readonly ILogger<ValidateWorkflowHandler> _logger;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;
    private readonly IWorkflowApplicationManagementService _workflowManagementService;
    private readonly ICurrentUserService _currentUserService;

    public ValidateWorkflowHandler(
        ILogger<ValidateWorkflowHandler> logger, 
        ISerializer serializer,
        IDeserializer deserializer,
        IWorkflowApplicationManagementService workflowManagementService,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        _workflowManagementService = workflowManagementService ?? throw new ArgumentNullException(nameof(workflowManagementService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<ValidationResponse>> Handle(ValidateWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var jsonString = _serializer.Serialize(request.Json);

            var result = await _workflowManagementService.ValidateJsonAsync(
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