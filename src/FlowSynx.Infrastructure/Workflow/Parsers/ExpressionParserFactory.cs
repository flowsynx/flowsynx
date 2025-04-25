namespace FlowSynx.Infrastructure.Workflow.Parsers;

public class ExpressionParserFactory : IExpressionParserFactory
{
    public IExpressionParser CreateParser(Dictionary<string, object?> taskOutputs)
        => new ExpressionParser(taskOutputs);
}