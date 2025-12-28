using FlowSynx.Domain.Enums;

namespace FlowSynx.Domain.ValueObjects;

public record GeneExecutionResult
{
    public GeneInstanceId GeneInstanceId { get; }
    public GeneExpressionStatus Status { get; }
    public object Result { get; }
    public System.Exception Error { get; }
    public TimeSpan ExecutionTime { get; }
    public Dictionary<string, object> Metrics { get; }

    public bool IsSuccess => Status == GeneExpressionStatus.Completed;

    public GeneExecutionResult(
        GeneInstanceId geneInstanceId,
        GeneExpressionStatus status,
        object result = null,
        System.Exception error = null,
        TimeSpan executionTime = default,
        Dictionary<string, object> metrics = null)
    {
        GeneInstanceId = geneInstanceId ?? throw new ArgumentNullException(nameof(geneInstanceId));
        Status = status;
        Result = result;
        Error = error;
        ExecutionTime = executionTime;
        Metrics = metrics ?? new Dictionary<string, object>();
    }
}