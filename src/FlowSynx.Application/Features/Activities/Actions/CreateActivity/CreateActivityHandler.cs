using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Activities.Actions.CreateActivity;

internal class CreateActivityHandler : IActionHandler<CreateActivityRequest, Result<CreateActivityResult>>
{
    private readonly ILogger<CreateActivityHandler> _logger;
    private readonly ISerializer _serializer;
    private readonly IWorkflowApplicationManagementService _managementService;
    private readonly ICurrentUserService _currentUserService;

    public CreateActivityHandler(
        ILogger<CreateActivityHandler> logger, 
        ISerializer serializer,
        IWorkflowApplicationManagementService managementService,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<CreateActivityResult>> Handle(CreateActivityRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var jsonString = _serializer.Serialize(request.Json);
            var activity = await _managementService.RegisterActivityAsync(_currentUserService.UserId(), jsonString);

            var response = new CreateActivityResult
            {
                Status = "created",
                Id = activity.Id,
                Name = activity.Name,
                Version = activity.Version,
                Namespace = activity.Namespace
            };

            return await Result<CreateActivityResult>.SuccessAsync(response);
        }
        catch (ValidationException vex)
        {
            var errorMessages = vex.Errors
                .SelectMany(kvp => kvp.Value.Select(msg => $"{kvp.Key}: {msg}"))
                .ToList();
            return await Result<CreateActivityResult>.FailAsync(errorMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return await Result<CreateActivityResult>.FailAsync(ex.Message);
        }
    }
}