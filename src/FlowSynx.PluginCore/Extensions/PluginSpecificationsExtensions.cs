using System.Reflection;

namespace FlowSynx.PluginCore.Extensions;

public static class PluginSpecificationsExtensions
{
    public static T ToObject<T>(this PluginSpecifications? source) where T : class, new()
    {
        var newInstance = new T();
        if (source is null) return newInstance;

        var someObjectType = newInstance.GetType();
        foreach (var item in source)
        {
            var property = someObjectType.GetProperty(item.Key, BindingFlags.Public 
                                                             | BindingFlags.Instance 
                                                             | BindingFlags.IgnoreCase);
            if (property != null)
                property.SetValue(newInstance, item.Value, null);
        }

        return newInstance;
    }
}