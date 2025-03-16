using FlowSynx.Application.Features.Workflows.Command.Execute;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Entities.Trigger;
using FlowSynx.Domain.Entities.Workflow;
using FlowSynx.Domain.Interfaces;
using FlowSynx.IO.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlowSynx.Application.Features.Workflows.Command.Add;

internal class AddWorkflowHandler : IRequestHandler<AddWorkflowRequest, Result<AddWorkflowResponse>>
{
    private readonly ILogger<AddWorkflowHandler> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly IWorkflowTriggerService _workflowTriggerService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IJsonDeserializer _jsonDeserializer;

    public AddWorkflowHandler(ILogger<AddWorkflowHandler> logger, IWorkflowService workflowService,
        IWorkflowTriggerService workflowTriggerService, ICurrentUserService currentUserService, IJsonDeserializer jsonDeserializer)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _workflowService = workflowService;
        _workflowTriggerService = workflowTriggerService;
        _currentUserService = currentUserService;
        _jsonDeserializer = jsonDeserializer;
    }

    public async Task<Result<AddWorkflowResponse>> Handle(AddWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            var workflowDefinition = _jsonDeserializer.Deserialize<WorkflowDefinition>(request.Definition);

            if (workflowDefinition == null)
                throw new Exception("Workflow definition must be not empty!");

            if (workflowDefinition.Name == null)
                throw new Exception("Workflow name shold have value!");

            var isWorkflowExist = await _workflowService.IsExist(_currentUserService.UserId, workflowDefinition.Name, cancellationToken);
            if (isWorkflowExist)
            {
                var workflowExistMessage = string.Format(Resources.AddWorkflowNameIsAlreadyExist, workflowDefinition.Name);
                _logger.LogWarning(workflowExistMessage);
                return await Result<AddWorkflowResponse>.FailAsync(workflowExistMessage);
            }

            var workflowEntity = new WorkflowEntity
            {
                Id = Guid.NewGuid(),
                UserId = _currentUserService.UserId,
                Name = workflowDefinition.Name,
                Definition = request.Definition,
            };

            await _workflowService.Add(workflowEntity, cancellationToken);

            foreach (var trigger in workflowDefinition.Triggers)
            {
                var workflowTrigger = new WorkflowTriggerEntity
                {
                    Id = Guid.NewGuid(),
                    WorkflowId = workflowEntity.Id,
                    UserId = _currentUserService.UserId,
                    Type = trigger.Type,
                    Details = trigger.Details,
                };

               await _workflowTriggerService.Add(workflowTrigger, cancellationToken);
            }

            var response = new AddWorkflowResponse 
            { 
                Id = workflowEntity.Id,
                Name = workflowDefinition.Name,
            };
            return await Result<AddWorkflowResponse>.SuccessAsync(response, Resources.AddConfigHandlerSuccessfullyAdded);
        }
        catch (JsonDeserializerException ex)
        {
            throw new Exception($"Json deserialization error: {ex.Message}");
        }
        catch (JsonReaderException ex)
        {
            throw new Exception($"Reader Error at Line {ex.LineNumber}, Position {ex.LinePosition}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return await Result<AddWorkflowResponse>.FailAsync(ex.Message);
        }
    }
}