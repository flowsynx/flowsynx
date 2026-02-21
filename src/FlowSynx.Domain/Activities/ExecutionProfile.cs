namespace FlowSynx.Domain.Activities;

public sealed class ExecutionProfile
{
    public static class Modes
    {
        public const string Synchronous = "synchronous";
        public const string Asynchronous = "asynchronous";
    }

    public const int DefaultPriority = 1;
    public const int DefaultTimeoutMilliseconds = 5000;

    public string DefaultOperation { get; set; } = string.Empty;
    public List<ExecutionCondition> Conditions { get; set; } = new();
    public int Priority { get; set; } = DefaultPriority;
    public string ExecutionMode { get; set; } = Modes.Synchronous;
    public int TimeoutMilliseconds { get; set; } = DefaultTimeoutMilliseconds;

    public ExecutionSettings ToSettings(string? operationOverride = null)
    {
        var operation = string.IsNullOrWhiteSpace(operationOverride)
            ? DefaultOperation?.Trim() ?? string.Empty
            : operationOverride.Trim();

        return new ExecutionSettings(
            Operation: operation,
            Mode: NormalizeMode(ExecutionMode),
            Priority: NormalizePriority(Priority),
            TimeoutMilliseconds: NormalizeTimeout(TimeoutMilliseconds));
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(DefaultOperation))
            errors.Add("ExecutionProfile.DefaultOperation is required.");

        if (Priority < 1)
            errors.Add("ExecutionProfile.Priority must be >= 1.");

        if (TimeoutMilliseconds < 1)
            errors.Add("ExecutionProfile.TimeoutMilliseconds must be >= 1.");

        var mode = (ExecutionMode ?? string.Empty).Trim();
        if (!string.Equals(mode, Modes.Synchronous, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(mode, Modes.Asynchronous, StringComparison.OrdinalIgnoreCase))
            errors.Add($"ExecutionProfile.ExecutionMode must be '{Modes.Synchronous}' or '{Modes.Asynchronous}'.");

        return errors;
    }

    private static int NormalizePriority(int priority) => priority < 1 ? DefaultPriority : priority;
    private static int NormalizeTimeout(int timeout) => timeout < 1 ? DefaultTimeoutMilliseconds : timeout;
    private static string NormalizeMode(string? mode) =>
        string.Equals(mode, Modes.Asynchronous, StringComparison.OrdinalIgnoreCase) ? Modes.Asynchronous : Modes.Synchronous;

    public readonly record struct ExecutionSettings(string Operation, string Mode, int Priority, int TimeoutMilliseconds);
}