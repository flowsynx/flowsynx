namespace FlowSynx.Application.Models;

public class ValidationWarning
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";
}