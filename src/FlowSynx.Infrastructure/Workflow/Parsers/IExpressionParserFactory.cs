using FlowSynx.Infrastructure.Secrets;

namespace FlowSynx.Infrastructure.Workflow.Parsers;

public interface IExpressionParserFactory
{
    IExpressionParser CreateParser(
        Dictionary<string, object?> taskOutputs, 
        Dictionary<string, object?>? variables,
        ISecretFactory? secretFactory = null);
}