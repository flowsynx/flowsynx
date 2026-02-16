using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.WorkflowApplications.Actions.CreateWorkflowApplication;

internal class CreateWorkflowApplicationHandler : IActionHandler<CreateWorkflowApplicationRequest, Result<CreateWorkflowApplicationResult>>
{
    private readonly ILogger<CreateWorkflowApplicationHandler> _logger;
    private readonly ISerializer _serializer;
    private readonly IWorkflowApplicationManagementService _managementService;
    private readonly ICurrentUserService _currentUserService;

    public CreateWorkflowApplicationHandler(
        ILogger<CreateWorkflowApplicationHandler> logger, 
        ISerializer serializer,
        IWorkflowApplicationManagementService managementService,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<CreateWorkflowApplicationResult>> Handle(CreateWorkflowApplicationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var jsonString = _serializer.Serialize(request.Json);
            var workflowApplication = await _managementService.RegisterWorkflowApplicationAsync(_currentUserService.UserId(), jsonString);

            var response = new CreateWorkflowApplicationResult
            {
                Status = "created",
                Id = workflowApplication.Id,
                Name = workflowApplication.Name,
                Namespace = workflowApplication.Namespace
            };

            return await Result<CreateWorkflowApplicationResult>.SuccessAsync(response);
        }
        catch (ValidationException vex)
        {
            var errorMessages = vex.Errors
                .SelectMany(kvp => kvp.Value.Select(msg => $"{kvp.Key}: {msg}"))
                .ToList();
            return await Result<CreateWorkflowApplicationResult>.FailAsync(errorMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return await Result<CreateWorkflowApplicationResult>.FailAsync(ex.Message);
        }
    }
}