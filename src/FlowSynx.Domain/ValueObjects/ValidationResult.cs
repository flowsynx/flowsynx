namespace FlowSynx.Domain.ValueObjects;

public record ValidationResult
{
    public bool IsValid { get; }
    public List<string> Errors { get; }
    public List<string> Warnings { get; }

    public ValidationResult(bool isValid, List<string> errors = null, List<string> warnings = null)
    {
        IsValid = isValid;
        Errors = errors ?? new List<string>();
        Warnings = warnings ?? new List<string>();
    }

    public void AddError(string error) => Errors.Add(error);
    public void AddWarning(string warning) => Warnings.Add(warning);
}