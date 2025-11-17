using System.Text.Json;
using FlowSynx.Application.Configuration.System.Workflow.Execution;
using FlowSynx.Application.Features.Workflows.Query.WorkflowDetails;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.PluginHost.Manager;
using FlowSynx.Application.Services;
using FlowSynx.Application.Wrapper;
using FlowSynx.Domain.Plugin;
using FlowSynx.PluginCore.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.WorkflowExecutions.Command.EnsureWorkflowPlugins;

internal sealed class EnsureWorkflowPluginsHandler : IRequestHandler<EnsureWorkflowPluginsRequest, Result<Unit>>
{
    private readonly ILogger<EnsureWorkflowPluginsHandler> _logger;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPluginService _pluginService;
    private readonly IPluginManager _pluginManager;
    private readonly ILocalization _localization;
    private readonly EnsureWorkflowPluginsConfiguration _ensureWorkflowPluginsConfiguration;

    public EnsureWorkflowPluginsHandler(
        ILogger<EnsureWorkflowPluginsHandler> logger,
        IMediator mediator,
        ICurrentUserService currentUserService,
        IPluginService pluginService,
        IPluginManager pluginManager,
        ILocalization localization,
        EnsureWorkflowPluginsConfiguration ensureWorkflowPluginsConfiguration)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(pluginService);
        ArgumentNullException.ThrowIfNull(pluginManager);
        ArgumentNullException.ThrowIfNull(localization);
        ArgumentNullException.ThrowIfNull(ensureWorkflowPluginsConfiguration);

        _logger = logger;
        _mediator = mediator;
        _currentUserService = currentUserService;
        _pluginService = pluginService;
        _pluginManager = pluginManager;
        _localization = localization;
        _ensureWorkflowPluginsConfiguration = ensureWorkflowPluginsConfiguration;
    }

    public async Task<Result<Unit>> Handle(EnsureWorkflowPluginsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            if (!_ensureWorkflowPluginsConfiguration.EnsureWorkflowPlugins)
            {
                _logger.LogInformation("Ensure workflow plugins feature is disabled. Skipping plugin installation for workflow '{WorkflowId}'.", request.WorkflowId);
                return await Result<Unit>.SuccessAsync(_localization.Get("Feature_WorkflowExecution_PluginEnsure_Disabled"));
            }

            var details = await _mediator.Send(new WorkflowDetailsRequest { WorkflowId = request.WorkflowId }, cancellationToken);
            if (!details.Succeeded || details.Data is null)
            {
                var msg = _localization.Get("Feature_WorkflowExecution_NoWorkflowFound", request.WorkflowId);
                return await Result<Unit>.FailAsync(msg);
            }

            // Expecting workflow definition text to be available. If not present, no-op.
            var workflow = details.Data.Workflow;
            if (workflow is null)
                return await Result<Unit>.SuccessAsync(_localization.Get("Feature_WorkflowExecution_NoDefinitionFound", request.WorkflowId));

            var required = ExtractPluginRequirements(workflow);

            // Deduplicate and skip if none found
            var toProcess = required
                .Distinct(StringTupleComparer.OrdinalIgnoreCase)
                .ToArray();

            if (toProcess.Length == 0)
                return await Result<Unit>.SuccessAsync(_localization.Get("Feature_WorkflowExecution_NoPluginsRequired", request.WorkflowId));

            foreach (var (type, version) in toProcess)
            {
                var effectiveVersion = string.IsNullOrWhiteSpace(version) ? "latest" : version.Trim();
                var exists = await _pluginService.IsExist(_currentUserService.UserId(), type, effectiveVersion, cancellationToken);
                if (exists)
                    continue;

                _logger.LogInformation("Plugin '{PluginType}' version '{Version}' not installed. Installing...", type, effectiveVersion);
                await _pluginManager.InstallAsync(type, effectiveVersion, cancellationToken);
                _logger.LogInformation("Plugin '{PluginType}' version '{Version}' installed successfully.", type, effectiveVersion);
            }

            return await Result<Unit>.SuccessAsync(_localization.Get("Feature_Plugin_Install_AddedSuccessfully"));
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex, "FlowSynx exception ensuring workflow plugins.");
            return await Result<Unit>.FailAsync(ex.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error ensuring workflow plugins.");
            return await Result<Unit>.FailAsync(ex.Message);
        }
    }

    // Traverses the JSON workflow to find objects bearing plugin "type" and "version" hints.
    // Heuristics:
    // - property names checked (case-insensitive): "Plugin" or "type" for the plugin id
    // - "version" for the version.
    // - If version missing/empty, we later treat it as "latest".
    private static IEnumerable<(string Type, string? Version)> ExtractPluginRequirements(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return Traverse(doc.RootElement);

        static IEnumerable<(string Type, string? Version)> Traverse(JsonElement root)
        {
            var results = new List<(string, string?)>();
            var stack = new Stack<JsonElement>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var el = stack.Pop();
                switch (el.ValueKind)
                {
                    case JsonValueKind.Object:
                        ExtractObject(el, results, stack);
                        break;
                    case JsonValueKind.Array:
                        foreach (var item in el.EnumerateArray())
                            stack.Push(item);
                        break;
                    default:
                        // Other JsonValueKind values are not relevant for plugin extraction.
                        break;
                }
            }

            return results;
        }

        static void ExtractObject(JsonElement obj, List<(string, string?)> results, Stack<JsonElement> stack)
        {
            string? type = null;
            string? version = null;

            foreach (var prop in obj.EnumerateObject())
            {
                var val = prop.Value;

                if (val.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                {
                    stack.Push(val);
                    continue;
                }

                if (val.ValueKind != JsonValueKind.String)
                    continue;

                var name = prop.Name;
                if (IsTypeProperty(name))
                {
                    type = val.GetString();
                }
                else if (IsVersionProperty(name))
                {
                    version = val.GetString();
                }
            }

            if (!string.IsNullOrWhiteSpace(type))
                results.Add((type!, version));
        }

        static bool IsTypeProperty(string name)
            => name.Equals("Plugin", StringComparison.OrdinalIgnoreCase);

        static bool IsVersionProperty(string name)
            => name.Equals("version", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class StringTupleComparer : IEqualityComparer<(string Type, string? Version)>
    {
        public static readonly StringTupleComparer OrdinalIgnoreCase = new();

        public bool Equals((string Type, string? Version) x, (string Type, string? Version) y)
            => string.Equals(x.Type, y.Type, StringComparison.OrdinalIgnoreCase)
               && string.Equals(x.Version ?? string.Empty, y.Version ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Type, string? Version) obj)
        {
            var h1 = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Type);
            var h2 = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Version ?? string.Empty);
            return HashCode.Combine(h1, h2);
        }
    }
}