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

    private List<ExecutionCondition> _conditions = [];
    private RetryPolicy _retryPolicy = new();

    public string DefaultOperation { get; set; } = string.Empty;

    public List<ExecutionCondition> Conditions
    {
        get => _conditions;
        set => _conditions = value ?? [];
    }

    public int Priority { get; set; } = DefaultPriority;

    /// <summary>
    /// Expected values: "synchronous", "asynchronous".
    /// </summary>
    public string ExecutionMode { get; set; } = Modes.Synchronous;

    public int TimeoutMilliseconds { get; set; } = DefaultTimeoutMilliseconds;

    public RetryPolicy RetryPolicy
    {
        get => _retryPolicy;
        set => _retryPolicy = value ?? new RetryPolicy();
    }

    public readonly record struct ExecutionSettings(
        string Operation,
        string Mode,
        int Priority,
        int TimeoutMilliseconds,
        RetryPolicy RetryPolicy);

    public ExecutionSettings ToSettings(string? operationOverride = null)
    {
        var operation = string.IsNullOrWhiteSpace(operationOverride)
            ? (DefaultOperation ?? string.Empty).Trim()
            : operationOverride.Trim();

        return new ExecutionSettings(
            Operation: operation,
            Mode: NormalizeMode(ExecutionMode),
            Priority: NormalizePriority(Priority),
            TimeoutMilliseconds: NormalizeTimeout(TimeoutMilliseconds),
            RetryPolicy: CloneRetryPolicy(RetryPolicy));
    }

    public IReadOnlyList<string> Validate()
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(DefaultOperation))
            errors.Add("ExecutionProfile.DefaultOperation is required.");

        if (Priority < 1)
            errors.Add("ExecutionProfile.Priority must be >= 1.");

        if (TimeoutMilliseconds < 1)
            errors.Add("ExecutionProfile.TimeoutMilliseconds must be >= 1.");

        var mode = (ExecutionMode ?? string.Empty).Trim();
        if (!string.Equals(mode, Modes.Synchronous, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(mode, Modes.Asynchronous, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"ExecutionProfile.ExecutionMode must be '{Modes.Synchronous}' or '{Modes.Asynchronous}'.");
        }

        if (RetryPolicy.MaxAttempts < 1)
            errors.Add("ExecutionProfile.RetryPolicy.MaxAttempts must be >= 1.");

        if (RetryPolicy.DelayMilliseconds < 0)
            errors.Add("ExecutionProfile.RetryPolicy.DelayMilliseconds must be >= 0.");

        if (RetryPolicy.BackoffMultiplier < 1.0f)
            errors.Add("ExecutionProfile.RetryPolicy.BackoffMultiplier must be >= 1.0.");

        if (RetryPolicy.MaxDelayMilliseconds < 0)
            errors.Add("ExecutionProfile.RetryPolicy.MaxDelayMilliseconds must be >= 0.");

        return errors;
    }

    private static int NormalizePriority(int priority) => priority < 1 ? DefaultPriority : priority;

    private static int NormalizeTimeout(int timeoutMilliseconds) =>
        timeoutMilliseconds < 1 ? DefaultTimeoutMilliseconds : timeoutMilliseconds;

    private static string NormalizeMode(string? mode)
    {
        mode = (mode ?? string.Empty).Trim();

        if (string.Equals(mode, Modes.Asynchronous, StringComparison.OrdinalIgnoreCase))
            return Modes.Asynchronous;

        // Default/fallback
        return Modes.Synchronous;
    }

    private static RetryPolicy CloneRetryPolicy(RetryPolicy policy) =>
        new()
        {
            MaxAttempts = policy.MaxAttempts,
            DelayMilliseconds = policy.DelayMilliseconds,
            BackoffMultiplier = policy.BackoffMultiplier,
            MaxDelayMilliseconds = policy.MaxDelayMilliseconds
        };
}