namespace FlowSync.Core.Configuration;

public interface IConfigurationManager
{
    ConfigurationItem GetSetting(string name);
    IEnumerable<ConfigurationItem> GetSettings();
    bool IsExist(string name);
    ConfigurationStatus AddSetting(ConfigurationItem configuration);
    bool DeleteSetting(string name);
}