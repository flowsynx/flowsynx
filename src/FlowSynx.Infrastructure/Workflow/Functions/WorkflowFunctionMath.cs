namespace FlowSynx.Infrastructure.Workflow.Functions;

public class WorkflowFunctionMath : IWorkflowFunction
{
    private readonly Func<object?, List<object>, object> _func;
    private readonly int _expectedArgs;

    public WorkflowFunctionMath(Func<object?, List<object>, object> func, int expectedArgs)
    {
        _func = func;
        _expectedArgs = expectedArgs;
    }

    public  void ValidateArguments(List<object> arguments)
    {
        if (arguments.Count != _expectedArgs)
        {
            throw new ArgumentException($"This transformation requires exactly {_expectedArgs} arguments.");
        }
    }

    public object Execute(object? value, List<object> arguments)
    {
        return _func(value, arguments);
    }
}