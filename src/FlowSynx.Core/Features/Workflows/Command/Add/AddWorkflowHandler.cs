using FlowSynx.Core.Services;
using FlowSynx.Core.Wrapper;
using FlowSynx.Domain.Entities.Workflow;
using FlowSynx.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Core.Features.Workflows.Command.Add;

internal class AddWorkflowHandler : IRequestHandler<AddWorkflowRequest, Result<AddWorkflowResponse>>
{
    private readonly ILogger<AddWorkflowHandler> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly ICurrentUserService _currentUserService;

    public AddWorkflowHandler(ILogger<AddWorkflowHandler> logger, IWorkflowService workflowService, 
        ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _workflowService = workflowService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<AddWorkflowResponse>> Handle(AddWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            var isWorkflowExist = await _workflowService.IsExist(_currentUserService.UserId, request.Name, cancellationToken);
            if (isWorkflowExist)
            {
                var workflowExistMessage = string.Format(Resources.AddWorkflowNameIsAlreadyExist, request.Name);
                _logger.LogWarning(workflowExistMessage);
                return await Result<AddWorkflowResponse>.FailAsync(workflowExistMessage);
            }

            var workflowDefination = new WorkflowEntity
            {
                Id = Guid.NewGuid(),
                UserId = _currentUserService.UserId,
                Name = request.Name,
                Definition = request.Template.ToString(),
            };

            await _workflowService.Add(workflowDefination, cancellationToken);
            var response = new AddWorkflowResponse { 
                Id = workflowDefination.Id,
                Name = request.Name,
            };
            return await Result<AddWorkflowResponse>.SuccessAsync(response, Resources.AddConfigHandlerSuccessfullyAdded);
        }
        catch (Exception ex)
        {
            return await Result<AddWorkflowResponse>.FailAsync(ex.Message);
        }
    }
}