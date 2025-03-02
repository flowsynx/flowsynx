namespace FlowSynx.Domain.Interfaces;

public interface IWorkflowFunction
{
    void ValidateArguments(List<object> arguments);
    object Execute(object? value, List<object> arguments);
}