using FlowSynx.Domain.Enums;
using FlowSynx.Domain.GeneInstances;

namespace FlowSynx.Domain.ValueObjects;

public record ExpressionResult
{
    public GeneInstanceId GeneInstanceId { get; }
    public ExpressionStatus Status { get; }
    public object ExpressedProtein { get; }     // Result 
    public System.Exception Error { get; }
    public TimeSpan ExpressionTime { get; }
    public Dictionary<string, object> ExpressionMetrics { get; }

    public bool IsExpressed => Status == ExpressionStatus.Expressed;

    public ExpressionResult(
        GeneInstanceId geneInstanceId,
        ExpressionStatus status,
        object result = null,
        System.Exception error = null,
        TimeSpan expressionTime = default,
        Dictionary<string, object> metrics = null)
    {
        GeneInstanceId = geneInstanceId ?? throw new ArgumentNullException(nameof(geneInstanceId));
        Status = status;
        ExpressedProtein = result;
        Error = error;
        ExpressionTime = expressionTime;
        ExpressionMetrics = metrics ?? new Dictionary<string, object>();
    }
}