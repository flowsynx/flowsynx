using FlowSynx.Core.Services;
using FlowSynx.Core.Wrapper;
using FlowSynx.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Core.Features.Workflows.Command.Delete;

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
            var workflow = await _workflowService.Get(_currentUserService.UserId, request.Name, cancellationToken);
            if (workflow == null)
                throw new Exception($"The workflow '{request.Name}' not found");

            var deleteResult = await _workflowService.Delete(workflow, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.DeleteConfigHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}