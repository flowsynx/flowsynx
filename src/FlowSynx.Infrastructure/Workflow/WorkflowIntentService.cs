using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Serialization;
using FlowSynx.Application.Workflow;
using FlowSynx.Infrastructure.AI;
using Newtonsoft.Json.Linq;

namespace FlowSynx.Infrastructure.Workflow;

public sealed class WorkflowIntentService : IWorkflowIntentService
{
    private readonly IAiFactory _aiFactory;
    private readonly IJsonDeserializer _deserializer;

    public WorkflowIntentService(
        IAiFactory aiFactory,
        IJsonDeserializer deserializer)
    {
        _aiFactory = aiFactory ?? throw new ArgumentNullException(nameof(aiFactory));
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
    }

    public async Task<(WorkflowDefinition Definition, string RawJson, string PlanSummary)> SynthesizeAsync(
        string goal,
        string? capabilitiesJson,
        CancellationToken cancellationToken)
    {
        var aiProvider = _aiFactory.GetDefaultProvider();
        var raw = await aiProvider.GenerateWorkflowJsonAsync(goal, capabilitiesJson, cancellationToken);

        // Optional: extract a "plan" if present; otherwise synthesize a brief one from the tasks.
        string plan = "Proposed workflow generated from intent.";
        try
        {
            var jo = JObject.Parse(raw);
            if (jo.TryGetValue("plan", out var planToken) && planToken.Type == JTokenType.String)
                plan = planToken!.Value<string>()!;
        }
        catch
        {
            // ignore plan extraction failure
        }

        var def = _deserializer.Deserialize<WorkflowDefinition>(raw);
        if (string.IsNullOrWhiteSpace(def.Name))
            def.Name = "auto-generated-workflow";

        return (def, raw, plan);
    }
}