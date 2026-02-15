using FlowSynx.Domain.Activities;

namespace FlowSynx.Domain.WorkflowApplications;

public class ApplicationValidation
{
    public string Schema { get; set; } = string.Empty;
    public List<ValidationRule> Rules { get; set; } = new();
}