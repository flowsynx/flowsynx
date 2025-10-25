namespace FlowSynx.Infrastructure.Workflow.Parsers;

public class ExpressionParserFactory : IExpressionParserFactory
{
    public IExpressionParser CreateParser(
        Dictionary<string, object?> taskOutputs, 
        Dictionary<string, object?>? variables) => new ExpressionParser(taskOutputs, variables);
}