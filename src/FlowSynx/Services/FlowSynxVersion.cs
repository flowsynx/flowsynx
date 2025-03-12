using FlowSynx.Application.Services;
using System.Reflection;

namespace FlowSynx.Services;

public class FlowSynxVersion : IVersion
{
    public string Version => GetApplicationVersion();

    #region Private function
    private string GetApplicationVersion()
    {
        var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
        return attributes.Length == 0 ? "" : ((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;
    }
    #endregion
}