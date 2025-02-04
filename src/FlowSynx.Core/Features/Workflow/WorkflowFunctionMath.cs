namespace FlowSynx.Core.Features.Workflow;

public class WorkflowFunctionMath : WorkflowFunction
{
    private readonly Func<object?, List<object>, object> _func;
    private readonly int _expectedArgs;

    public WorkflowFunctionMath(Func<object?, List<object>, object> func, int expectedArgs)
    {
        _func = func;
        _expectedArgs = expectedArgs;
    }

    public override void ValidateArguments(List<object> arguments)
    {
        if (arguments.Count != _expectedArgs)
        {
            throw new ArgumentException($"This transformation requires exactly {_expectedArgs} arguments.");
        }
    }

    public override object Transform(object? value, List<object> arguments)
    {
        return _func(value, arguments);
    }
}