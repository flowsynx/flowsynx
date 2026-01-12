namespace FlowSynx.Infrastructure.Runtime.Expression;

public class ConditionEvaluator
{
    public bool Evaluate(string condition, object context)
    {
        // Very simple evaluator - in reality, use Roslyn or a proper expression engine
        if (string.IsNullOrWhiteSpace(condition))
            return true;

        condition = condition.Trim();

        // Check for simple true/false
        if (condition.Equals("true", StringComparison.OrdinalIgnoreCase))
            return true;
        if (condition.Equals("false", StringComparison.OrdinalIgnoreCase))
            return false;

        // Check for existence patterns
        if (condition.StartsWith("exists(") && condition.EndsWith(")"))
        {
            var path = condition.Substring(7, condition.Length - 8);
            return CheckExists(path, context);
        }

        return false;
    }

    private bool CheckExists(string path, object context)
    {
        // Simple existence check
        try
        {
            var parts = path.Split('.');
            object current = context;

            foreach (var part in parts)
            {
                var property = current.GetType().GetProperty(part);
                if (property == null)
                    return false;

                current = property.GetValue(current);
                if (current == null)
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}