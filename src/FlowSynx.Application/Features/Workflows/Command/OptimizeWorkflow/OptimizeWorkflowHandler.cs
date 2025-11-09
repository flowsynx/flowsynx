using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Application.Serialization;
using FlowSynx.Application.Services;
using FlowSynx.Application.Workflow;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Workflow;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlowSynx.Application.Features.Workflows.Command.OptimizeWorkflow;

internal class OptimizeWorkflowHandler : IRequestHandler<OptimizeWorkflowRequest, Result<OptimizeWorkflowResponse>>
{
    private readonly ILogger<OptimizeWorkflowHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkflowService _workflowService;
    private readonly IWorkflowOptimizationService _optimizationService;
    private readonly IWorkflowSchemaValidator _schemaValidator;
    private readonly ILocalization _localization;
    private readonly IJsonDeserializer _deserializer;

    public OptimizeWorkflowHandler(
        ILogger<OptimizeWorkflowHandler> logger,
        ICurrentUserService currentUserService,
        IWorkflowService workflowService,
        IWorkflowOptimizationService optimizationService,
        IWorkflowSchemaValidator schemaValidator,
        ILocalization localization,
        IJsonDeserializer deserializer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
        _optimizationService = optimizationService ?? throw new ArgumentNullException(nameof(optimizationService));
        _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
    }

    public async Task<Result<OptimizeWorkflowResponse>> Handle(OptimizeWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            if (!Guid.TryParse(request.WorkflowId, out var wid))
                throw new FlowSynxException((int)ErrorCode.WorkflowGetItem, "Invalid workflow id.");

            var wf = await _workflowService.Get(_currentUserService.UserId(), wid, cancellationToken);
            if (wf is null)
                throw new FlowSynxException((int)ErrorCode.WorkflowNotFound, "Workflow not found.");

            var def = _deserializer.Deserialize<WorkflowDefinition>(wf.Definition);
            var (optimized, explanation) = await _optimizationService.OptimizeAsync(def, cancellationToken);

            var optimizedJson = JsonConvert.SerializeObject(optimized, Formatting.Indented);
            await _schemaValidator.ValidateAsync(request.SchemaUrl ?? wf.SchemaUrl, optimizedJson, cancellationToken);

            if (request.ApplyChanges)
            {
                wf.Definition = optimizedJson;
                wf.SchemaUrl = request.SchemaUrl ?? wf.SchemaUrl;
                await _workflowService.Update(wf, cancellationToken);
            }

            var response = new OptimizeWorkflowResponse
            {
                OptimizedWorkflowJson = optimizedJson,
                Explanation = explanation,
                WorkflowId = request.ApplyChanges ? wf.Id : null
            };

            return await Result<OptimizeWorkflowResponse>.SuccessAsync(response,
                _localization.Get("Feature_Workflow_Optimize_Success"));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<OptimizeWorkflowResponse>.FailAsync(ex.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<OptimizeWorkflowResponse>.FailAsync(ex.Message);
        }
    }
}