namespace FlowSynx.Core.Features.Workflow;

public abstract class WorkflowFunction
{
    public abstract void ValidateArguments(List<object> arguments);
    public abstract object Transform(object? value, List<object> arguments);
}