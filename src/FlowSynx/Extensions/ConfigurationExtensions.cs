namespace FlowSynx.Extensions;

/// <summary>
/// Centralized configuration binding helpers to avoid duplication across extension classes.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Bind a configuration section to a configuration object and return it.
    /// Simplifies repeated pattern of creating config instances and binding sections.
    /// </summary>
    public static T BindSection<T>(this IConfiguration configuration, string sectionName) where T : new()
    {
        var section = new T();
        configuration.GetSection(sectionName).Bind(section);
        return section;
    }
}