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
using System.Text;

namespace FlowSynx.Application.Features.Workflows.Command.Add;

internal class AddWorkflowHandler : IRequestHandler<AddWorkflowRequest, Result<AddWorkflowResponse>>
{
    private readonly ILogger<AddWorkflowHandler> _logger;
    private readonly ITransactionService _transactionService;
    private readonly IWorkflowService _workflowService;
    private readonly IWorkflowTriggerService _workflowTriggerService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly IWorkflowValidator _workflowValidator;

    public AddWorkflowHandler(ILogger<AddWorkflowHandler> logger, ITransactionService transactionService,
        IWorkflowService workflowService, IWorkflowTriggerService workflowTriggerService, 
        ICurrentUserService currentUserService, IJsonDeserializer jsonDeserializer, 
        IWorkflowValidator workflowValidator)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(transactionService);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(workflowTriggerService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(jsonDeserializer);
        _logger = logger;
        _transactionService = transactionService;
        _workflowService = workflowService;
        _workflowTriggerService = workflowTriggerService;
        _currentUserService = currentUserService;
        _jsonDeserializer = jsonDeserializer;
        _workflowValidator = workflowValidator;
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

            ValidateWorkflow(workflowDefinition.Tasks);

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

            await _transactionService.TransactionAsync(async () =>
            {
                await _workflowService.Add(workflowEntity, cancellationToken);

                foreach (var trigger in workflowDefinition.Configuration.Triggers)
                {
                    var workflowTrigger = new WorkflowTriggerEntity
                    {
                        Id = Guid.NewGuid(),
                        WorkflowId = workflowEntity.Id,
                        UserId = _currentUserService.UserId,
                        Type = trigger.Type,
                        Status = WorkflowTriggerStatus.Active,
                        Properties = trigger.Properties,
                    };

                    await _workflowTriggerService.Add(workflowTrigger, cancellationToken);
                }
            }, cancellationToken);

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

    private void ValidateWorkflow(List<WorkflowTask> workflowTasks)
    {
        var hasWorkflowPipelinesDuplicateNames = _workflowValidator.HasDuplicateNames(workflowTasks);
        if (hasWorkflowPipelinesDuplicateNames)
            throw new Exception("There is a duplicated pipeline name in the workflow pipelines.");

        var missingDependencies = _workflowValidator.AllDependenciesExist(workflowTasks);
        if (missingDependencies.Any())
        {
            var sb = new StringBuilder();
            sb.AppendLine("Invalid workflow: missing dependencies.. There are list of missing dependencies:");
            sb.AppendLine(string.Join(",", missingDependencies));
            throw new Exception(sb.ToString());
        }

        var validation = _workflowValidator.CheckCyclic(workflowTasks);
        if (validation.Cyclic)
        {
            var sb = new StringBuilder();
            sb.AppendLine("The workflow has cyclic dependencies. Please resolve them and try again!. There are Cyclic:");
            sb.AppendLine(string.Join(" -> ", validation.CyclicNodes));

            throw new Exception(sb.ToString());
        }
    }
}