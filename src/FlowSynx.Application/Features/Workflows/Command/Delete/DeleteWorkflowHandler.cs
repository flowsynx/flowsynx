using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Interfaces;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Workflows.Command.Delete;

internal class DeleteWorkflowHandler : IRequestHandler<DeleteWorkflowRequest, Result<Unit>>
{
    private readonly ILogger<DeleteWorkflowHandler> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly ICurrentUserService _currentUserService;

    public DeleteWorkflowHandler(ILogger<DeleteWorkflowHandler> logger, ICurrentUserService currentUserService,
        IWorkflowService workflowService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(workflowService);
        _logger = logger;
        _currentUserService = currentUserService;
        _workflowService = workflowService;
    }

    public async Task<Result<Unit>> Handle(DeleteWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var workflowId = Guid.Parse(request.Id);
            var workflow = await _workflowService.Get(_currentUserService.UserId, workflowId, cancellationToken);
            if (workflow == null)
                throw new FlowSynxException((int)ErrorCode.WorkflowNotFound, $"The workflow with id '{request.Id}' not found");

            await _workflowService.Delete(workflow, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.DeleteConfigHandlerSuccessfullyDeleted);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Unit>.FailAsync(ex.ToString());
        }
    }
}