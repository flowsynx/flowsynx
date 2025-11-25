namespace FlowSynx.Infrastructure.Workflow.Expressions;

/// <summary>
/// Interface for function evaluators in expressions
/// </summary>
public interface IFunctionEvaluator
{
    /// <summary>
    /// The name of the function (case-insensitive)
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Evaluates the function with the given arguments
    /// </summary>
    /// <param name="args">Evaluated arguments passed to the function</param>
    /// <returns>The result of the function evaluation</returns>
    object? Evaluate(List<object?> args);
}