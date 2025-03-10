﻿using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Core.Wrapper;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Core.Services;

namespace FlowSynx.Core.Features.Workflows.Query.Details;

internal class WorkflowDetailsHandler : IRequestHandler<WorkflowDetailsRequest, Result<WorkflowDetailsResponse>>
{
    private readonly ILogger<WorkflowDetailsHandler> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly ICurrentUserService _currentUserService;

    public WorkflowDetailsHandler(ILogger<WorkflowDetailsHandler> logger,
        IWorkflowService workflowService, ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _workflowService = workflowService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<WorkflowDetailsResponse>> Handle(WorkflowDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            if (string.IsNullOrEmpty(request.Name))
                ArgumentNullException.ThrowIfNull(request.Name);

            var workflow = await _workflowService.Get(_currentUserService.UserId, request.Name, cancellationToken);
            if (workflow is null) 
                throw new Exception($"The workflow '{request.Name}' not found");

            var response = new WorkflowDetailsResponse
            {
                Id = workflow.Id,
                Name = workflow.Name,
                Template = workflow.Template
            };
            _logger.LogInformation("Workflow details is executed successfully.");
            return await Result<WorkflowDetailsResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<WorkflowDetailsResponse>.FailAsync(ex.Message);
        }
    }
}