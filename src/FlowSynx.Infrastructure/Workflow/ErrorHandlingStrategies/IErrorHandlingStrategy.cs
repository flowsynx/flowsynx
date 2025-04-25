namespace FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;

public interface IErrorHandlingStrategy
{
    Task<ErrorHandlingResult> HandleAsync(
        ErrorHandlingContext context,
        CancellationToken cancellation);
}
