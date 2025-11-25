using FlowSynx.Application.Localizations;
using FlowSynx.Domain;
using FlowSynx.Application.Serialization;
using FlowSynx.Application.Services;
using FlowSynx.Application.Workflow;
using FlowSynx.Domain.Wrapper;
using FlowSynx.Domain.Workflow;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Workflows.Command.GenerateFromIntent;

internal class GenerateFromIntentHandler : IRequestHandler<GenerateFromIntentRequest, Result<GenerateFromIntentResponse>>
{
    private readonly ILogger<GenerateFromIntentHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkflowService _workflowService;
    private readonly IWorkflowIntentService _intentService;
    private readonly IWorkflowValidator _workflowValidator;
    private readonly IWorkflowSchemaValidator _schemaValidator;
    private readonly ILocalization _localization;

    public GenerateFromIntentHandler(
        ILogger<GenerateFromIntentHandler> logger,
        ICurrentUserService currentUserService,
        IWorkflowService workflowService,
        IWorkflowIntentService intentService,
        IWorkflowValidator workflowValidator,
        IWorkflowSchemaValidator schemaValidator,
        ILocalization localization)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
        _intentService = intentService ?? throw new ArgumentNullException(nameof(intentService));
        _workflowValidator = workflowValidator ?? throw new ArgumentNullException(nameof(workflowValidator));
        _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
    }

    public async Task<Result<GenerateFromIntentResponse>> Handle(GenerateFromIntentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var (definition, rawJson, plan) = await _intentService.SynthesizeAsync(
                request.Goal, request.CapabilitiesJson, cancellationToken);

            // Optional overrides
            if (!string.IsNullOrWhiteSpace(request.NameOverride))
                definition.Name = request.NameOverride;

            // Guardrails: schema + semantic validation
            await _schemaValidator.ValidateAsync(request.SchemaUrl, rawJson, cancellationToken);
            await _workflowValidator.ValidateAsync(definition, cancellationToken);

            Guid? workflowId = null;
            if (request.AutoCreate)
            {
                var entity = new WorkflowEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = _currentUserService.UserId(),
                    Name = definition.Name ?? "auto-generated-workflow",
                    Definition = rawJson,
                    SchemaUrl = request.SchemaUrl
                };

                await _workflowService.Add(entity, cancellationToken);
                workflowId = entity.Id;
            }

            var response = new GenerateFromIntentResponse
            {
                WorkflowId = workflowId,
                Name = definition.Name,
                WorkflowJson = rawJson,
                Plan = plan,
                SchemaUrl = request.SchemaUrl
            };

            return await Result<GenerateFromIntentResponse>.SuccessAsync(response,
                _localization.Get("Feature_Workflow_GenerateFromIntent_Success"));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex, ex.Message);
            return await Result<GenerateFromIntentResponse>.FailAsync(ex.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return await Result<GenerateFromIntentResponse>.FailAsync(ex.Message);
        }
    }
}