namespace FlowSynx.Infrastructure.Workflow.Parsers;

public interface IExpressionParserFactory
{
    IExpressionParser CreateParser(Dictionary<string, object?> taskOutputs);
}