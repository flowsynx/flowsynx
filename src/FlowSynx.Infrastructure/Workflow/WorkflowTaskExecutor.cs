using FlowSynx.Application.Features.Workflows.Command.Execute;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.Infrastructure.PluginHost;

namespace FlowSynx.Infrastructure.Workflow;

public class WorkflowTaskExecutor : IWorkflowTaskExecutor
{
    private readonly IPluginTypeService _pluginTypeService;
    private readonly IPlaceholderReplacer _placeholderReplacer;

    public WorkflowTaskExecutor(
        IPluginTypeService pluginTypeService,
        IPlaceholderReplacer placeholderReplacer)
    {
        _pluginTypeService = pluginTypeService;
        _placeholderReplacer = placeholderReplacer;
    }

    public async Task<object?> ExecuteAsync(string userId, WorkflowTask task, IExpressionParser parser, CancellationToken cancellationToken)
    {
        var plugin = await _pluginTypeService.Get(userId, task.Type, cancellationToken).ConfigureAwait(false);

        var resolvedParameters = task.Parameters ?? new Dictionary<string, object?>();
        _placeholderReplacer.ReplacePlaceholdersInParameters(resolvedParameters, parser);
        var pluginParameters = resolvedParameters.ToPluginParameters();

        return await plugin.ExecuteAsync(pluginParameters, cancellationToken).ConfigureAwait(false);
    }
}
