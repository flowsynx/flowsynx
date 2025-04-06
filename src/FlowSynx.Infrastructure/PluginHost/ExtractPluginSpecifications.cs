using FlowSynx.Domain.Plugin;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.PluginCore;

namespace FlowSynx.Infrastructure.PluginHost;

public class ExtractPluginSpecifications: IExtractPluginSpecifications
{
    public List<PluginSpecification> GetPluginSpecification(IPlugin? plugin)
    {
        if (plugin == null)
            return new List<PluginSpecification>();
        
        var specificationsType = plugin.SpecificationsType;
        return specificationsType
            .GetProperties()
            .Select(property => new PluginSpecification
            {
                Name = property.Name,
                Type = property.PropertyType.GetPrimitiveType(),
                IsReadable = property.CanRead,
                IsWritable = property.CanWrite,
                DeclaringType = property.DeclaringType?.FullName,
                IsRequired = Attribute.IsDefined(property, typeof(RequiredMemberAttribute))
            }).ToList();
    }
}