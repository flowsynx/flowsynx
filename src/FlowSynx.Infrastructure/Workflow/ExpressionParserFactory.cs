namespace FlowSynx.Infrastructure.Workflow;

public class ExpressionParserFactory : IExpressionParserFactory
{
    public IExpressionParser CreateParser(Dictionary<string, object?> taskOutputs)
        => new ExpressionParser(taskOutputs);
}