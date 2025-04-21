namespace FlowSynx.Infrastructure.Workflow;

public interface IExpressionParserFactory
{
    IExpressionParser CreateParser(Dictionary<string, object?> taskOutputs);
}