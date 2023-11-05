namespace FlowSync.Abstractions.Extensions;

public static class ObjectExtensions
{
    public static T CastToObject<T>(this IDictionary<string, object>? dict) where T : new()
    {
        var t = new T();

        if (dict == null)
            return t;

        var properties = t.GetType().GetProperties();

        foreach (var property in properties)
        {
            if (!dict.Any(x => x.Key.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase)))
                continue;

            var item = dict.First(x => x.Key.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase));
            var tPropertyType = t.GetType().GetProperty(property.Name)!.PropertyType;
            var newT = Nullable.GetUnderlyingType(tPropertyType) ?? tPropertyType;
            var newA = Convert.ChangeType(item.Value, newT);
            t.GetType().GetProperty(property.Name)!.SetValue(t, newA, null);
        }
        return t;
    }
}