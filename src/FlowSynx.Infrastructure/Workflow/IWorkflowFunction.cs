namespace FlowSynx.Infrastructure.Workflow;

public interface IWorkflowFunction
{
    void ValidateArguments(List<object> arguments);
    object Execute(object? value, List<object> arguments);
}