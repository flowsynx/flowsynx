using FlowSynx.Application.Features.Workflows.Command.Add;
using FlowSynx.Application.Features.Workflows.Command.Execute;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Workflows.Command.Update;

internal class UpdateWorkflowHandler : IRequestHandler<UpdateWorkflowRequest, Result<Unit>>
{
    private readonly ILogger<UpdateWorkflowHandler> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly ICurrentUserService _currentUserService;

    public UpdateWorkflowHandler(ILogger<UpdateWorkflowHandler> logger, ICurrentUserService currentUserService,
        IWorkflowService workflowService, IJsonDeserializer jsonDeserializer)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(workflowService);
        _logger = logger;
        _currentUserService = currentUserService;
        _workflowService = workflowService;
        _jsonDeserializer = jsonDeserializer;
    }

    public async Task<Result<Unit>> Handle(UpdateWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            var workflowId = Guid.Parse(request.Id);
            var workflow = await _workflowService.Get(_currentUserService.UserId, workflowId, cancellationToken);
            if (workflow == null)
                throw new Exception($"The workflow with id '{request.Id}' not found");

            var workflowDefinition = _jsonDeserializer.Deserialize<WorkflowDefinition>(request.Definition);

            if (workflowDefinition == null)
                throw new Exception("Workflow definition must be not empty!");

            if (workflowDefinition.Name == null)
                throw new Exception("Workflow name shold have value!");

            if (!string.Equals(workflow.Name, workflowDefinition.Name, StringComparison.OrdinalIgnoreCase))
            {
                var isWorkflowExist = await _workflowService.IsExist(_currentUserService.UserId, workflowDefinition.Name, cancellationToken);
                if (isWorkflowExist)
                {
                    var workflowExistMessage = string.Format(Resources.AddWorkflowNameIsAlreadyExist, workflowDefinition.Name);
                    _logger.LogWarning(workflowExistMessage);
                    return await Result<Unit>.FailAsync(workflowExistMessage);
                }
            }

            workflow.Name = workflowDefinition.Name;
            workflow.Definition = request.Definition.ToString();

            await _workflowService.Update(workflow, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.DeleteConfigHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}