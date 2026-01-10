namespace FlowSynx.BuildingBlocks.Results;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Messages { get; set; } = new();
    public DateTime GeneratedAtUtc { get; set; }

    public static ValidationResult Fail()
    {
        return new ValidationResult { IsValid = false };
    }

    public static ValidationResult Fail(string message)
    {
        return new ValidationResult { IsValid = false, Messages = new List<string> { message } };
    }

    public static ValidationResult Fail(List<string> messages)
    {
        return new ValidationResult { IsValid = false, Messages = messages };
    }

    public static Task<ValidationResult> FailAsync()
    {
        return Task.FromResult(Fail());
    }

    public static Task<ValidationResult> FailAsync(string message)
    {
        return Task.FromResult(Fail(message));
    }

    public static Task<ValidationResult> FailAsync(List<string> messages)
    {
        return Task.FromResult(Fail(messages));
    }

    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    public static ValidationResult Success(string message)
    {
        return new ValidationResult { IsValid = true, Messages = new List<string> { message } };
    }

    public static Task<ValidationResult> SuccessAsync(string message)
    {
        return Task.FromResult(Success(message));
    }
}