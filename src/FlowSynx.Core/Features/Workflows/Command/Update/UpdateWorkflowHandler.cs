using FlowSynx.Core.Services;
using FlowSynx.Core.Wrapper;
using FlowSynx.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Core.Features.Workflows.Command.Update;

internal class UpdateWorkflowHandler : IRequestHandler<UpdateWorkflowRequest, Result<Unit>>
{
    private readonly ILogger<UpdateWorkflowHandler> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateWorkflowHandler(ILogger<UpdateWorkflowHandler> logger, ICurrentUserService currentUserService,
        IWorkflowService workflowService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(workflowService);
        _logger = logger;
        _currentUserService = currentUserService;
        _workflowService = workflowService;
    }

    public async Task<Result<Unit>> Handle(UpdateWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var workflow = await _workflowService.Get(_currentUserService.UserId, request.Id, cancellationToken);
            if (workflow == null)
                throw new Exception($"The workflow with id '{request.Id}' not found");

            workflow.Name = request.Name;
            workflow.Template = request.Template.ToString();

            await _workflowService.Update(workflow, cancellationToken);
            return await Result<Unit>.SuccessAsync(Resources.DeleteConfigHandlerSuccessfullyDeleted);
        }
        catch (Exception ex)
        {
            return await Result<Unit>.FailAsync(new List<string> { ex.Message });
        }
    }
}