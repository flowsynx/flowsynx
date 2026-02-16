using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Workflows.Actions.CreateWorkflow;

internal class CreateWorkflowHandler : IActionHandler<CreateWorkflowRequest, Result<CreateWorkflowResult>>
{
    private readonly ILogger<CreateWorkflowHandler> _logger;
    private readonly ISerializer _serializer;
    private readonly IWorkflowApplicationManagementService _managementService;
    private readonly ICurrentUserService _currentUserService;

    public CreateWorkflowHandler(
        ILogger<CreateWorkflowHandler> logger, 
        ISerializer serializer,
        IWorkflowApplicationManagementService managementService,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _managementService = managementService ?? throw new ArgumentNullException(nameof(managementService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<CreateWorkflowResult>> Handle(CreateWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var jsonString = _serializer.Serialize(request.Json);
            var workflow = await _managementService.RegisterWorkflowAsync(_currentUserService.UserId(), jsonString);

            var response = new CreateWorkflowResult
            {
                Status = "created",
                Id = workflow.Id,
                Name = workflow.Name,
                Namespace = workflow.Namespace
            };

            return await Result<CreateWorkflowResult>.SuccessAsync(response);
        }
        catch (ValidationException vex)
        {
            var errorMessages = vex.Errors
                .SelectMany(kvp => kvp.Value.Select(msg => $"{kvp.Key}: {msg}"))
                .ToList();
            return await Result<CreateWorkflowResult>.FailAsync(errorMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return await Result<CreateWorkflowResult>.FailAsync(ex.Message);
        }
    }
}