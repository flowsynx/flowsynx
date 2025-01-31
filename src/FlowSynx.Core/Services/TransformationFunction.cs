namespace FlowSynx.Core.Services;

public abstract class TransformationFunction
{
    public abstract void ValidateArguments(List<object> arguments);
    public abstract object Transform(object? value, List<object> arguments);
}