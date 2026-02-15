namespace FlowSynx.Domain.Workflows;

public class OutputMapping
{
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // activityId.result.field
    public string? Transform { get; set; } // jsonpath, jq, template
}