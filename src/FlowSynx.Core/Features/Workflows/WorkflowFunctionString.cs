//namespace FlowSynx.Core.Features.Workflow;

//public class WorkflowFunctionString : WorkflowFunction
//{
//    private readonly Func<object?, List<object>, object> _func;
//    private readonly int _minArgs;
//    private readonly int _maxArgs;

//    public WorkflowFunctionString(Func<object?, List<object>, object> func, int minArgs = 0, int maxArgs = 0)
//    {
//        _func = func;
//        _minArgs = minArgs;
//        _maxArgs = maxArgs;
//    }

//    public override void ValidateArguments(List<object> arguments)
//    {
//        if (arguments.Count < _minArgs || arguments.Count > _maxArgs)
//        {
//            throw new ArgumentException($"This transformation requires between {_minArgs} and {_maxArgs} arguments.");
//        }
//    }

//    public override object Transform(object? value, List<object> arguments)
//    {
//        return _func(value, arguments);
//    }
//}