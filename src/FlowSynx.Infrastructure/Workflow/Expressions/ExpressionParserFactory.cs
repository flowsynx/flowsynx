using FlowSynx.Infrastructure.Secrets;

namespace FlowSynx.Infrastructure.Workflow.Expressions;

public class ExpressionParserFactory : IExpressionParserFactory
{
    public IExpressionParser CreateParser(
        Dictionary<string, object?> taskOutputs, 
        Dictionary<string, object?>? variables,
        ISecretFactory? secretFactory = null) => new ExpressionParser(taskOutputs, variables, secretFactory);
}