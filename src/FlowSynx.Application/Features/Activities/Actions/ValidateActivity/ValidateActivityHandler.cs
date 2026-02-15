using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Activities.Actions.ValidateActivity;

internal class ValidateActivityHandler : IActionHandler<ValidateActivityRequest, Result<ValidationResponse>>
{
    private readonly ILogger<ValidateActivityHandler> _logger;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;
    private readonly IWorkflowApplicationExecutionService _workflowExecutionService;
    private readonly ICurrentUserService _currentUserService;

    public ValidateActivityHandler(
        ILogger<ValidateActivityHandler> logger, 
        ISerializer serializer,
        IDeserializer deserializer,
        IWorkflowApplicationExecutionService workflowExecutionService,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        _workflowExecutionService = workflowExecutionService ?? throw new ArgumentNullException(nameof(workflowExecutionService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<ValidationResponse>> Handle(ValidateActivityRequest request, CancellationToken cancellationToken)
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