﻿using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Workflow;
using FlowSynx.Application.Localizations;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowTaskExecutionDetails;

internal class WorkflowTaskExecutionDetailsHandler : 
    IRequestHandler<WorkflowTaskExecutionDetailsRequest, Result<WorkflowTaskExecutionDetailsResponse>>
{
    private readonly ILogger<WorkflowTaskExecutionDetailsHandler> _logger;
    private readonly IWorkflowTaskExecutionService _workflowTaskExecutionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public WorkflowTaskExecutionDetailsHandler(
        ILogger<WorkflowTaskExecutionDetailsHandler> logger,
        IWorkflowTaskExecutionService workflowTaskExecutionService,
        ICurrentUserService currentUserService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowTaskExecutionService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _workflowTaskExecutionService = workflowTaskExecutionService;
        _currentUserService = currentUserService;
        _localization = localization;
    }

    public async Task<Result<WorkflowTaskExecutionDetailsResponse>> Handle(
        WorkflowTaskExecutionDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflowExecutionId = Guid.Parse(request.WorkflowExecutionId);
            var workflowTaskExecutionId = Guid.Parse(request.WorkflowTaskExecutionId);

            var workflowTaskExecution = await _workflowTaskExecutionService.Get(workflowId, 
                workflowExecutionId, workflowTaskExecutionId, cancellationToken);

            if (workflowTaskExecution is null)
            {
                var message = _localization.Get("Feature_WorkflowTaskExecution_Details_TaskExecutionNotFound", request.WorkflowId);
                throw new FlowSynxException((int)ErrorCode.WorkflowExecutionTaskNotFound, message);
            }

            var response = new WorkflowTaskExecutionDetailsResponse
            {
                Id = workflowTaskExecution.Id,
                Status = workflowTaskExecution.Status,
                Message = workflowTaskExecution.Message,
                StartTime = workflowTaskExecution.StartTime,
                EndTime = workflowTaskExecution.EndTime,
            };
            _logger.LogInformation(_localization.Get("Feature_WorkflowTaskExecution_Details_DataRetrievedSuccessfully"));
            return await Result<WorkflowTaskExecutionDetailsResponse>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<WorkflowTaskExecutionDetailsResponse>.FailAsync(ex.ToString());
        }
    }
}