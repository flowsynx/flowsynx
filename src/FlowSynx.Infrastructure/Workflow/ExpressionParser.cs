namespace FlowSynx.Infrastructure.Workflow;

public class ExpressionParser
{
    private Dictionary<string, object?> _outputs;

    public ExpressionParser(Dictionary<string, object?> outputs)
    {
        _outputs = outputs;
    }

    public object? Parse(string? expression)
    {
        if (expression == null)
            return expression;

        expression = expression.Trim();

        const string prefix = "$[outputs('";
        const string suffix = "')]";

        if (expression.StartsWith("$[outputs('") && expression.EndsWith("')]"))
        {
            string outputKey = expression.Substring(prefix.Length, expression.Length - prefix.Length - suffix.Length);

            if (_outputs.ContainsKey(outputKey))
            {
                return _outputs[outputKey];
            }
            else
            {
                throw new KeyNotFoundException($"Output '{outputKey}' not found.");
            }
        }

        return expression;
    }
}