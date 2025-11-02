using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Application.Serialization;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Workflow;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Workflows.Query.WorkflowDetails;

internal class WorkflowDetailsHandler : IRequestHandler<WorkflowDetailsRequest, Result<WorkflowDetailsResponse>>
{
    private readonly ILogger<WorkflowDetailsHandler> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IJsonSanitizer _jsonSanitizer;
    private readonly IJsonParser _jsonParser;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ILocalization _localization;

    public WorkflowDetailsHandler(
        ILogger<WorkflowDetailsHandler> logger,
        IWorkflowService workflowService, 
        ICurrentUserService currentUserService,
        IJsonSanitizer jsonSanitizer,
        IJsonParser jsonParser,
        IJsonSerializer jsonSerializer,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(jsonSanitizer);
        ArgumentNullException.ThrowIfNull(jsonParser);
        ArgumentNullException.ThrowIfNull(jsonSerializer);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _workflowService = workflowService;
        _currentUserService = currentUserService;
        _jsonSanitizer = jsonSanitizer;
        _jsonParser = jsonParser;
        _jsonSerializer = jsonSerializer;
        _localization = localization;
    }

    public async Task<Result<WorkflowDetailsResponse>> Handle(WorkflowDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var workflowId = Guid.Parse(request.WorkflowId);
            var workflow = await _workflowService.Get(_currentUserService.UserId, workflowId, cancellationToken);
            if (workflow is null)
            {
                var message = _localization.Get("Feature_Workflow_Details_WorkflowNotFound", request.WorkflowId);
                throw new FlowSynxException((int)ErrorCode.WorkflowNotFound, message);
            }

            var sanitized = _jsonSanitizer.Sanitize(workflow.Definition);
            var workflowObject = _jsonParser.ParseObject(sanitized);
            var workflowDef = new
            {
                Schema = workflow.SchemaUrl,
                Workflow = workflowObject
            };

            var definitionJson = _jsonSerializer.Serialize(workflowDef);

            var response = new WorkflowDetailsResponse
            {
                Id = workflow.Id,
                Name = workflow.Name,
                Workflow = definitionJson
            };
            _logger.LogInformation(_localization.Get("Feature_Workflow_Details_DataRetrievedSuccessfully"));
            return await Result<WorkflowDetailsResponse>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<WorkflowDetailsResponse>.FailAsync(ex.ToString());
        }
    }
}
