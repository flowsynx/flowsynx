using FlowSynx.Domain.Activities;

namespace FlowSynx.Domain.Workflows;

public class WorkflowValidation
{
    public string Schema { get; set; } = string.Empty;
    public List<ValidationRule> Rules { get; set; } = new();
}