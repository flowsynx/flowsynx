using FlowSync.Core.Configuration;
using FlowSync.Core.Enums;
using FlowSync.Core.Wrapper;

namespace FlowSync.Core.Services;

public interface IConfigurationManager
{
    ConfigurationItem GetSetting(string name);
    IEnumerable<ConfigurationItem> GetSettings();
    bool IsExist(string name);
    ConfigurationStatus AddSetting(ConfigurationItem configuration);
    bool DeleteSetting(string name);
}