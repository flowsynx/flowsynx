namespace FlowSynx.Extensions;

public static class ConfigurationExtensions
{
    public static T BindSection<T>(this IConfiguration configuration, string sectionName) where T : new()
    {
        var section = new T();
        configuration.GetSection(sectionName).Bind(section);
        return section;
    }
}